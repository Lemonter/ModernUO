using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;
using Server.Spells.Necromancy;
using Server.Spells.Chivalry;
using Server.Spells.Bushido;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonCombat;
using Server.Systems.MahaonMining;
using Server.Systems.MahaonProfessions;
using Server.Systems.MahaonRecipes;
using Server.Systems.MahaonRaids;
using Server.Targeting;

namespace Server.Systems.MahaonBots;

/// <summary>Mage-specific AI — spell rotation, buffs, escape spells, necromancy, party healing target selection.</summary>
public partial class BotController
{

    private static bool TryCastNecromancy(PlayerMobile mage)
    {
        if (!TransformationSpellHelper.UnderTransformation(mage) &&
            Utility.RandomDouble() < BotTuning.MageTransformChance &&
            mage.Skills[SkillName.Necromancy].Value >= 60 &&
            mage.Mana >= new LichFormSpell(mage).GetMana())
        {
            new LichFormSpell(mage).Cast();
            return true;
        }

        if (mage.Skills[SkillName.SpiritSpeak].Value >= 30 &&
            Utility.RandomDouble() < BotTuning.MageSummonFamiliarChance &&
            mage.Mana >= new AnimateDeadSpell(mage).GetMana() && FindRaisableCorpse(mage) != null)
        {
            new AnimateDeadSpell(mage).Cast();
            return true;
        }

        Spell spell = null;

        if (mage.Mana >= new PainSpikeSpell(mage).GetMana())
        {
            spell = new PainSpikeSpell(mage);
        }
        else if (mage.Mana >= new StrangleSpell(mage).GetMana())
        {
            spell = new StrangleSpell(mage);
        }
        else if (mage.Mana >= new WitherSpell(mage).GetMana())
        {
            spell = new WitherSpell(mage);
        }

        if (spell == null)
        {
            return false;
        }

        spell.Cast();
        return true;
    }

    private static Corpse FindRaisableCorpse(Mobile bot)
    {
        if (bot.Map == null)
        {
            return null;
        }

        foreach (var corpse in bot.Map.GetItemsInRange<Corpse>(bot.Location, 8))
        {
            if (!corpse.Deleted && corpse.Owner is BaseCreature { Summoned: false })
            {
                return corpse;
            }
        }

        return null;
    }

    // -- Mage support: real spellcasting, real reagents, real mana ------------------------
    //
    // A cast just sets Caster.Target to a pending SpellTarget (see GreaterHeal/Lightning
    // OnCast) — for a real player that becomes an interactive cursor; for a bot we resolve
    // it ourselves via Target.Invoke(caster, target), the same mechanism BaseAI's HealerAI
    // uses. Reagents and mana are consumed by the spell itself, same as any player cast.

    private const double HealThreshold = 0.7; // heal below this fraction of max HP
    private const int ManaReserveForHeal = 20; // keep this much mana in reserve for emergencies

    /// <summary>
    ///     Caught inside kite range — reach for a real escape tool instead of just walking
    ///     away (which TryMageKite alone could never win with an equally-fast pursuer).
    ///     Priority: Teleport clear > drop a wall in their path > paralyze them outright.
    ///     Each option gates on real skill/mana/reagents, same as every other spell here —
    ///     no shortcuts, just an actual decision a player would make in this spot.
    /// </summary>
    /// <summary>
    ///     Combat-only summon, tiered by real Magery skill — Blade Spirits at 30+, Energy
    ///     Vortex at 50+, Summon Daemon at 80+ (each gate also needs the real mana and a
    ///     free follower slot, same as a player would). Deliberately NOT maintained outside
    ///     combat like the Ranger's tamed pet — reuses the same SummonedPet field, so the
    ///     existing "dismiss when not fighting" logic at the top of Dispatch already handles
    ///     that on its own; nothing extra needed here for that part.
    /// </summary>
    private static bool TryMageSummonPet(PlayerMobile mage, BotProfile profile)
    {
        if (profile.SummonedPet?.Deleted == false && profile.SummonedPet.Alive)
        {
            return false; // already got one out
        }

        if (!IsFighting(mage) || mage.Map == null)
        {
            return false; // combat-only — never summon just to have one standing around
        }

        // The cast from a previous tick might have just resolved — claim it before trying
        // to cast another one on top of it.
        foreach (var creature in mage.Map.GetMobilesInRange<BaseCreature>(mage.Location, 4))
        {
            if (creature.ControlMaster == mage && creature.Alive && !creature.Deleted &&
                creature is BladeSpirits or EnergyVortex or SummonedDaemon)
            {
                profile.SummonedPet = creature;
                creature.ControlTarget = mage.Combatant;
                creature.ControlOrder = OrderType.Attack;
                creature.Combatant = mage.Combatant;
                creature.Warmode = true;
                return true;
            }
        }

        if (Core.Now < profile.NextSummonAttempt)
        {
            return false; // still waiting on the last cast's target to resolve — don't spam it
        }

        var skill = mage.Skills[SkillName.Magery].Value;
        profile.NextSummonAttempt = Core.Now + TimeSpan.FromSeconds(4);

        if (skill >= 80 && mage.Followers + 5 <= mage.FollowersMax &&
            mage.Mana >= new SummonDaemonSpell(mage).GetMana())
        {
            new SummonDaemonSpell(mage).Cast(); // self-targeting — resolves immediately, no ground target needed
            return true;
        }

        if (skill >= 50 && mage.Followers + 1 <= mage.FollowersMax &&
            mage.Mana >= new EnergyVortexSpell(mage).GetMana())
        {
            profile.PendingGroundTarget = mage.Location;
            new EnergyVortexSpell(mage).Cast();
            return true;
        }

        if (skill >= 30 && mage.Followers + 1 <= mage.FollowersMax &&
            mage.Mana >= new BladeSpiritsSpell(mage).GetMana())
        {
            profile.PendingGroundTarget = mage.Location;
            new BladeSpiritsSpell(mage).Cast();
            return true;
        }

        return false;
    }

    private static bool TryMageEscapeSpell(PlayerMobile mage, BotProfile profile, Mobile target, double magerySkill)
    {
        // Teleport (3rd circle) — the cleanest option when it's available: instantly clear
        // the melee range instead of trying to outwalk something moving at the same speed.
        if (magerySkill >= 50 && mage.Mana >= new TeleportSpell(mage).GetMana() + ManaReserveForHeal)
        {
            var escapePoint = FindEscapePoint(mage, target);
            if (escapePoint != null)
            {
                profile.PendingGroundTarget = escapePoint;
                new TeleportSpell(mage).Cast();
                return true;
            }
        }

        // No clean teleport spot (boxed in, or just don't know/can't afford it) — drop a
        // field between us and them. Doesn't remove them from melee range this instant,
        // but blocks the tile they'd need to close through, buying room to actually run.
        if (magerySkill >= 45 && Utility.RandomDouble() < BotTuning.MageWallChance)
        {
            var wallPoint = FindWallPoint(mage, target);

            if (wallPoint != null)
            {
                if (mage.Mana >= new PoisonFieldSpell(mage).GetMana() + ManaReserveForHeal)
                {
                    profile.PendingGroundTarget = wallPoint;
                    new PoisonFieldSpell(mage).Cast();
                    return true;
                }

                if (mage.Mana >= new FireFieldSpell(mage).GetMana() + ManaReserveForHeal)
                {
                    profile.PendingGroundTarget = wallPoint;
                    new FireFieldSpell(mage).Cast();
                    return true;
                }
            }
        }

        // Last resort short of just walking — lock them down directly so distance can
        // actually open up on the next few movement ticks.
        if (!target.Paralyzed && magerySkill >= 65 &&
            mage.Mana >= new ParalyzeSpell(mage).GetMana() + ManaReserveForHeal)
        {
            new ParalyzeSpell(mage).Cast();
            return true;
        }

        return false; // nothing affordable/known — fall through to the normal spell ladder
    }

    /// <summary>Picks a walkable point a few tiles away, roughly opposite the threat, for
    /// an escape Teleport to land on.</summary>
    private static Point3D? FindEscapePoint(Mobile mage, Mobile target)
    {
        if (mage.Map == null)
        {
            return null;
        }

        var dx = mage.X - target.X;
        var dy = mage.Y - target.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomMinMax(-1, 1);
            dy = Utility.RandomMinMax(-1, 1);
        }

        for (var distance = MageIdealRange; distance >= 3; distance--)
        {
            var x = mage.X + System.Math.Sign(dx) * distance;
            var y = mage.Y + System.Math.Sign(dy) * distance;
            var z = mage.Map.GetAverageZ(x, y);
            var loc = new Point3D(x, y, z);

            if (mage.Map.CanSpawnMobile(loc) && mage.InLOS(loc))
            {
                return loc;
            }
        }

        return null;
    }

    /// <summary>Picks the tile directly between the mage and the target — where a field
    /// spell actually blocks the chase instead of just sitting off to one side.</summary>
    private static Point3D? FindWallPoint(Mobile mage, Mobile target)
    {
        if (mage.Map == null)
        {
            return null;
        }

        var dx = System.Math.Sign(target.X - mage.X);
        var dy = System.Math.Sign(target.Y - mage.Y);

        if (dx == 0 && dy == 0)
        {
            return null; // standing on top of each other — no useful "between" tile
        }

        var x = mage.X + dx;
        var y = mage.Y + dy;
        var z = mage.Map.GetAverageZ(x, y);
        var loc = new Point3D(x, y, z);

        return mage.Map.CanFit(x, y, z, 16) ? loc : null;
    }

    private static void DoMageAction(PlayerMobile mage, BotProfile profile)
    {
        TryGainStat(mage, StatType.Int);

        // Resolve a pending cast from last tick, if any.
        if (mage.Target is Target pendingTarget)
        {
            ResolvePendingSpellTarget(mage, profile, pendingTarget);
            return;
        }

        if (mage.Poisoned && mage.Mana >= new CureSpell(mage).GetMana())
        {
            new CureSpell(mage).Cast();
            return;
        }

        var wounded = FindMostWoundedPartyMember(mage, profile);

        if (wounded != null && mage.Mana >= new GreaterHealSpell(mage).GetMana())
        {
            profile.PendingHealTarget = wounded;
            new GreaterHealSpell(mage).Cast();
            return;
        }

        if (IsFighting(mage) && Utility.RandomDouble() < BotTuning.MageCastsThisTickChance)
        {
            var magerySkill = mage.Skills[SkillName.Magery].Value;
            var target = mage.Combatant;

            // Caught at melee range — this is what TryMageKite's plain walk-away step was
            // handling alone before. A real mage has better tools than just legging it:
            // teleport clear, drop a wall in the pursuer's path, or paralyze them outright.
            // Falls through to the normal spell ladder below if none of these are
            // affordable/known — the plain kite step in TryMageKite is still the last resort.
            if (mage.GetDistanceToSqrt(target.Location) < MageTooCloseRange &&
                TryMageEscapeSpell(mage, profile, target, magerySkill))
            {
                return;
            }

            if (TryMageSummonPet(mage, profile))
            {
                return;
            }

            // Dispel any summoned creature on sight — a nuke that just gets replaced by
            // another summon isn't worth casting.
            if (target is BaseCreature { Summoned: true } && magerySkill >= 60 &&
                mage.Mana >= new DispelSpell(mage).GetMana())
            {
                new DispelSpell(mage).Cast();
                return;
            }

            if (profile.PrefersNecromancy && mage.Skills[SkillName.Necromancy].Value >= 30 &&
                TryCastNecromancy(mage))
            {
                return;
            }

            // Warriors run a big Str/Int gap by design (our own profession stat spread) —
            // Mind Blast scales off exactly that gap, so it's a real anti-warrior pick, not
            // just flavor text.
            var isHighGapTarget = System.Math.Abs(target.RawStr - target.RawInt) >= 30;

            if (magerySkill >= 45 && Utility.RandomDouble() < BotTuning.MageFieldChance)
            {
                if (mage.Mana >= new PoisonFieldSpell(mage).GetMana() + ManaReserveForHeal)
                {
                    new PoisonFieldSpell(mage).Cast();
                    return;
                }

                if (mage.Mana >= new FireFieldSpell(mage).GetMana() + ManaReserveForHeal)
                {
                    new FireFieldSpell(mage).Cast();
                    return;
                }
            }

            if (isHighGapTarget && magerySkill >= 70 && mage.Mana >= new MindBlastSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new MindBlastSpell(mage).Cast();
                return;
            }

            if (!target.Poisoned && magerySkill >= 40 && Utility.RandomDouble() < BotTuning.MagePoisonChance &&
                mage.Mana >= new PoisonSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new PoisonSpell(mage).Cast();
                return;
            }

            if (!target.Paralyzed && magerySkill >= 65 && Utility.RandomDouble() < BotTuning.MageParalyzeChance &&
                mage.Mana >= new ParalyzeSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new ParalyzeSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 30 && Utility.RandomDouble() < BotTuning.MageDebuffChance)
            {
                var debuff = Utility.RandomList(0, 1, 2);
                var debuffCost = debuff switch
                {
                    0 => new WeakenSpell(mage).GetMana(),
                    1 => new ClumsySpell(mage).GetMana(),
                    _ => new FeeblemindSpell(mage).GetMana()
                };

                if (mage.Mana >= debuffCost + ManaReserveForHeal)
                {
                    switch (debuff)
                    {
                        case 0: new WeakenSpell(mage).Cast(); return;
                        case 1: new ClumsySpell(mage).Cast(); return;
                        default: new FeeblemindSpell(mage).Cast(); return;
                    }
                }
            }

            if (TryDispelNearbyField(mage, profile))
            {
                return;
            }

            // Reach for the strongest damage spell the mage can actually afford and has
            // the Magery for — falls back down the ladder instead of always trying the top.
            if (magerySkill >= 95 && mage.Mana >= new FlameStrikeSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new FlameStrikeSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 90 && mage.Mana >= new ExplosionSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new ExplosionSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 80 && mage.Mana >= new EnergyBoltSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new EnergyBoltSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 50 && mage.Mana >= new FireballSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new FireballSpell(mage).Cast();
                return;
            }

            if (mage.Mana >= new LightningSpell(mage).GetMana() + ManaReserveForHeal)
            {
                new LightningSpell(mage).Cast();
                return;
            }

            if (mage.Mana >= new MagicArrowSpell(mage).GetMana())
            {
                new MagicArrowSpell(mage).Cast();
                return;
            }
        }

        // Nothing urgent — top off buffs on downtime before just standing around.
        if ((!IsFighting(mage)) && Core.Now >= profile.NextBuffTime)
        {
            if (TryCastBuff(mage, profile))
            {
                return;
            }
        }

        // Still nothing urgent — acquire a target like everyone else so there's something
        // to eventually nuke, but don't melee it.
        if (!IsFighting(mage))
        {
            if (mage.Map != null)
            {
                foreach (var creature in mage.Map.GetMobilesInRange<BaseCreature>(mage.Location, 10))
                {
                    if (BotController.IsHuntable(mage, creature))
                    {
                        mage.Combatant = creature;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Buffs self first (Bless/Strength/Agility/Cunning/Protection/Magic Reflect, one
    ///     per call so it spreads over a few ticks like a real player buffing up would),
    ///     then a wounded-adjacent party member if self is already topped up. Cooldown gets
    ///     refreshed either way so this doesn't get rolled every single idle tick.
    /// </summary>
    private static bool TryCastBuff(PlayerMobile mage, BotProfile profile)
    {
        profile.NextBuffTime = Core.Now + TimeSpan.FromMinutes(2);

        Mobile buffTarget = mage;
        if (Utility.RandomBool() && profile.Party != null)
        {
            foreach (var member in profile.Party.Members)
            {
                if (member != mage && member.Alive && !member.Deleted)
                {
                    buffTarget = member;
                    break;
                }
            }
        }

        var castSelf = buffTarget == mage;

        if (castSelf && mage.Mana >= new MagicReflectSpell(mage).GetMana() && Utility.RandomBool())
        {
            new MagicReflectSpell(mage).Cast(); // no target phase — applies immediately
            return true;
        }

        if (mage.Mana >= new BlessSpell(mage).GetMana())
        {
            profile.PendingHealTarget = buffTarget; // reuse the same "who" slot buffs resolve against
            new BlessSpell(mage).Cast();
            return true;
        }

        if (mage.Mana >= new ProtectionSpell(mage).GetMana())
        {
            profile.PendingHealTarget = buffTarget;
            new ProtectionSpell(mage).Cast();
            return true;
        }

        if (mage.Mana >= new StrengthSpell(mage).GetMana())
        {
            profile.PendingHealTarget = buffTarget;
            new StrengthSpell(mage).Cast();
            return true;
        }

        return false;
    }

    private static bool TryDispelNearbyField(PlayerMobile mage, BotProfile profile)
    {
        if (mage.Map == null || mage.Mana < new DispelFieldSpell(mage).GetMana())
        {
            return false;
        }

        foreach (var item in mage.Map.GetItemsInRange<Item>(mage.Location, 8))
        {
            if (item is FireFieldItem or PoisonField && !item.Deleted)
            {
                profile.PendingFieldTarget = item;
                new DispelFieldSpell(mage).Cast();
                return true;
            }
        }

        return false;
    }

    private static void ResolvePendingSpellTarget(PlayerMobile mage, BotProfile profile, Target pendingTarget)
    {
        // Item-targeted spells (Dispel Field targets the field itself, not a Mobile).
        if (pendingTarget is ITargetingSpell<Item> necroItemTarget)
        {
            var corpse = FindRaisableCorpse(mage);
            if (corpse == null)
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
                return;
            }

            pendingTarget.Invoke(mage, corpse);
            return;
        }

        if (pendingTarget is ISpellTarget<Item> itemSpellTarget)
        {
            var fieldItem = profile.PendingFieldTarget;
            profile.PendingFieldTarget = null;

            if (itemSpellTarget.Spell is DispelFieldSpell && fieldItem?.Deleted == false)
            {
                pendingTarget.Invoke(mage, fieldItem);
            }
            else
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            }

            return;
        }

        // Ground-targeted spells (fields, teleport, combat summons) all implement
        // ISpellTarget<IPoint3D> instead of ISpellTarget<Mobile> — the actual spell class
        // itself only implements ITargetingSpell<IPoint3D> (no .Spell member on that one;
        // it's the interface the spell exposes, not the interface the wrapping Target
        // exposes), so this has to check ISpellTarget<IPoint3D> to read .Spell at all.
        //
        // Fields default to landing on the combatant (offensive use) unless
        // TryMageEscapeSpell already set PendingGroundTarget for the defensive "wall
        // between us" cast. Teleport/Blade Spirits/Energy Vortex always resolve via
        // PendingGroundTarget (set by TryMageEscapeSpell or TryMageSummonPet respectively)
        // — if that's missing for some reason, better to eat the reagents on a canceled
        // cast than target blind.
        if (pendingTarget is ISpellTarget<IPoint3D> groundSpellTarget)
        {
            if (groundSpellTarget.Spell is FireFieldSpell or PoisonFieldSpell && mage.Map != null &&
                profile.PendingGroundTarget is { } wallPoint)
            {
                profile.PendingGroundTarget = null;
                pendingTarget.Invoke(mage, new LandTarget(wallPoint, mage.Map));
            }
            else if (groundSpellTarget.Spell is FireFieldSpell or PoisonFieldSpell &&
                IsFighting(mage) && mage.Map != null)
            {
                pendingTarget.Invoke(mage, new LandTarget(mage.Combatant.Location, mage.Map));
            }
            else if (groundSpellTarget.Spell is TeleportSpell or BladeSpiritsSpell or EnergyVortexSpell &&
                mage.Map != null && profile.PendingGroundTarget is { } summonPoint)
            {
                profile.PendingGroundTarget = null;
                pendingTarget.Invoke(mage, new LandTarget(summonPoint, mage.Map));
            }
            else
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            }

            return;
        }

        if (pendingTarget is ITargetingSpell<Mobile> necroTarget)
        {
            var necroResolved = IsFighting(mage) ? mage.Combatant : null;

            if (necroResolved == null)
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
                return;
            }

            pendingTarget.Invoke(mage, necroResolved);
            return;
        }

        if (pendingTarget is not ISpellTarget<Mobile> spellTarget)
        {
            pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            return;
        }

        Mobile resolved = spellTarget.Spell switch
        {
            GreaterHealSpell or HealSpell or BlessSpell or ProtectionSpell or StrengthSpell =>
                profile.PendingHealTarget?.Deleted == false && profile.PendingHealTarget.Alive
                    ? profile.PendingHealTarget
                    : mage,
            CureSpell => mage,
            LightningSpell or MagicArrowSpell or FireballSpell or EnergyBoltSpell or
                MindBlastSpell or FlameStrikeSpell or ExplosionSpell or PoisonSpell or
                ParalyzeSpell or DispelSpell or WeakenSpell or ClumsySpell or FeeblemindSpell =>
                IsFighting(mage) ? mage.Combatant : null,
            _ => null
        };

        // Помощь другому боту — единственный источник положительных отношений, и до сих
        // пор он не был подключён вовсе: OnHelped не вызывался ниоткуда, поэтому счёт мог
        // только падать, а союзы между гильдиями не складывались в принципе.
        if (resolved != null && resolved != mage && resolved is PlayerMobile)
        {
            BotRelationships.OnHelped(resolved, mage);
        }

        profile.PendingHealTarget = null;

        if (resolved == null)
        {
            pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            return;
        }

        pendingTarget.Invoke(mage, resolved);
    }

    private static Mobile FindMostWoundedPartyMember(Mobile mage, BotProfile profile)
    {
        Mobile worst = null;
        var worstRatio = HealThreshold;

        var selfRatio = mage.HitsMax > 0 ? (double)mage.Hits / mage.HitsMax : 1.0;
        if (mage.Alive && selfRatio < worstRatio)
        {
            worst = mage;
            worstRatio = selfRatio;
        }

        if (profile.Party != null)
        {
            foreach (var member in profile.Party.Members)
            {
                if (member == mage || !member.Alive || member.Deleted || member.HitsMax <= 0)
                {
                    continue;
                }

                var ratio = (double)member.Hits / member.HitsMax;
                if (ratio < worstRatio)
                {
                    worst = member;
                    worstRatio = ratio;
                }
            }
        }

        return worst;
    }
}
