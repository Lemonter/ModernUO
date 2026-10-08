using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;
using Server.Targeting;

namespace Server.Systems.MahaonCombat;

public enum AnimalTrainingCategory : byte
{
    Resistances = 0,   // Сопротивления — random resist type per tick until all capped
    CombatSkills = 1,  // Боевые техники (на манекене) — Wrestling/Tactics/Anatomy/Parry
    Stats = 2,         // Физическая подготовка — Str/Dex/Int
    Magic = 3          // Магия — raises the pet's real Magery skill; only if trainer knows Magery
}

public enum HerdingSpecialization : byte
{
    Resistances = 0,
    CombatSkills = 1,
    Stats = 2,
    Magic = 3
}

/// <summary>
///     Which curated spell pool a magic-trained pet draws from (see
///     AnimalTrainingSystem.SpellGroups) — chosen once when Magic training starts, switchable
///     any time by re-opening the Magic category. Only ONE group is ever active per pet: this
///     is a focus, not an unlock-everything grind — a Support-focused pet never learns to
///     nuke, a Damage-focused one never learns to buff.
/// </summary>
public enum MagicFocus : byte
{
    Support = 0,   // buffs/heals cast on the owner
    Debuff = 1,    // curses cast on the pet's current combatant
    Damage = 2,    // direct-damage/DoT spells cast on the pet's current combatant
    Summon = 3     // summons a temporary ally near the pet
}

/// <summary>
///     Full rework of what a Shepherd's Crook does to a player's own tamed pet — see
///     ShepherdsCrook.cs for the entry point (double-clicking your own controlled
///     creature now opens AnimalTrainingGump instead of the plain herd-move target).
///
///     Once started, training runs entirely in the background via a recurring Timer —
///     the trainer does NOT need to stay nearby, matching the request that they can walk
///     off and do other things while the pet trains. Each tick:
///       1. Looks for a stackable Food item within a few tiles of the pet and consumes
///          one unit. No food found = training stops.
///       2. If the current category has nothing left to raise (all resists at cap, all
///          three stats at their trainer-defined ceiling, etc), training stops.
///       3. Grants exp scaled by the crafting material of the crook used to START
///          training (captured at start, not re-checked per tick — matches "чем круче
///          материал посоха тем больше опыта за тик").
///       4. Shows "*обучается*" overhead in a random hue each tick.
///
///     Magic is the one category gated behind the TRAINER's own skill — see
///     CanTrainMagic. It raises the pet's real SkillName.Magery AND SkillName.EvalInt
///     together (so whatever AI the creature already has picks Magery up naturally, and
///     EvalInt feeds the same vanilla AOS spell-damage formula every player mage already
///     benefits from — "training increases cast power" for free, no custom damage hacks
///     needed) and flags the pet for a mana-cost premium — reagents are already free for
///     every non-player caster in this engine (Spell.ConsumeReagents already special-cases
///     !Caster.Player), so that half of "без реагентов" needed zero extra work here. The
///     premium starts at 10x and tapers down to 2x as Magery approaches 100 (see
///     GetManaCostMultiplier) — training genuinely lowers the cost, it just never fully
///     matches a real caster paying reagents.
///
///     A magic-trained pet also picks ONE MagicFocus (Support/Debuff/Damage/Summon, see
///     that enum) — SpellcasterCheck below only ever casts from that one curated group
///     (SpellGroups), switchable any time by re-choosing Magic in AnimalTrainingGump.
/// </summary>
public static class AnimalTrainingSystem
{
    private static readonly Dictionary<BaseCreature, (AnimalTrainingCategory category, Mobile trainer, double expPerTick, Timer timer)>
        Active = new();

    private static readonly HashSet<BaseCreature> MagicTrainedPets = new();

    public static bool IsMagicTrained(BaseCreature pet) => MagicTrainedPets.Contains(pet);

    // Not persisted, same as Active/MagicTrainedPets/PhysicalBaseline — in-memory only,
    // resets on restart, cheap to re-pick through normal play (same convention as the rest
    // of this system's per-session state).
    private static readonly Dictionary<BaseCreature, MagicFocus> PetMagicFocus = new();

    public static MagicFocus GetMagicFocus(BaseCreature pet) => PetMagicFocus.GetValueOrDefault(pet, MagicFocus.Damage);

    // Curated, hand-picked subsets of the real Magery spell list — deliberately excludes
    // pure utility/travel/thief spells (Teleport, Recall, GateTravel, Unlock, MagicTrap,
    // Mark, Reveal, Invisibility, Incognito, Polymorph, Telekinesis, WallOfStone,
    // CreateFood, NightSight, Dispel/DispelField/MassDispel, MagicReflect kept under
    // Support since reflecting damage back genuinely helps the owner) — none of those make
    // sense for an unattended background auto-caster. Ordered roughly by circle.
    public static readonly Type[] SupportSpells =
    {
        typeof(ReactiveArmorSpell), typeof(HealSpell), typeof(AgilitySpell), typeof(CunningSpell),
        typeof(CureSpell), typeof(ProtectionSpell), typeof(BlessSpell), typeof(StrengthSpell),
        typeof(ArchCureSpell), typeof(GreaterHealSpell), typeof(ArchProtectionSpell), typeof(MagicReflectSpell)
    };

    public static readonly Type[] DebuffSpells =
    {
        typeof(ClumsySpell), typeof(FeeblemindSpell), typeof(WeakenSpell), typeof(CurseSpell),
        typeof(ManaDrainSpell), typeof(ParalyzeSpell), typeof(MassCurseSpell), typeof(ManaVampireSpell)
    };

    public static readonly Type[] DamageSpells =
    {
        typeof(MagicArrowSpell), typeof(HarmSpell), typeof(FireballSpell), typeof(PoisonSpell),
        typeof(LightningSpell), typeof(FireFieldSpell), typeof(MindBlastSpell), typeof(PoisonFieldSpell),
        typeof(EnergyBoltSpell), typeof(ExplosionSpell), typeof(ParalyzeFieldSpell), typeof(ChainLightningSpell),
        typeof(FlameStrikeSpell), typeof(MeteorSwarmSpell), typeof(EarthquakeSpell)
    };

    public static readonly Type[] SummonSpells =
    {
        typeof(BladeSpiritsSpell), typeof(SummonCreatureSpell), typeof(AirElementalSpell),
        typeof(EarthElementalSpell), typeof(FireElementalSpell), typeof(WaterElementalSpell),
        typeof(SummonDaemonSpell), typeof(EnergyVortexSpell)
    };

    public static Type[] SpellGroup(MagicFocus focus) => focus switch
    {
        MagicFocus.Support => SupportSpells,
        MagicFocus.Debuff  => DebuffSpells,
        MagicFocus.Damage  => DamageSpells,
        MagicFocus.Summon  => SummonSpells,
        _                  => DamageSpells
    };

    /// <summary>Mana-cost multiplier for a magic-trained pet's casts — see Spell.ScaleMana.
    /// 10x at 0 Magery down to 2x at 100 Magery; linear in between.</summary>
    public static double GetManaCostMultiplier(BaseCreature pet) =>
        10.0 - Math.Clamp(pet.Skills[SkillName.Magery].Value, 0, 100) / 100.0 * 8.0;

    // Circle is a fixed, caster-independent property per spell class — cached once instead
    // of constructing a throwaway Spell instance on every UI refresh. Populated lazily the
    // first time any spell type is looked up.
    private static readonly Dictionary<Type, int> SpellCircleCache = new();

    private static int GetSpellCircle(Type spellType, Mobile dummyCaster)
    {
        if (SpellCircleCache.TryGetValue(spellType, out var circle))
        {
            return circle;
        }

        circle = Activator.CreateInstance(spellType, dummyCaster, null) is MagerySpell ms ? (int)ms.Circle + 1 : 1;
        SpellCircleCache[spellType] = circle;
        return circle;
    }

    /// <summary>For UI (MahaonAnimalLoreGump) — every spell in the pet's active focus group
    /// paired with whether its circle is currently reachable at the pet's real Magery
    /// value, using the exact same circleLimit formula CastRandom already gates casts
    /// with, so "shown as known" and "actually castable" never disagree.</summary>
    public static IEnumerable<(Type spellType, bool known)> GetFocusSpellKnowledge(BaseCreature pet)
    {
        var circleLimit = (int)(pet.Skills[SkillName.Magery].Value / 10) + 1;

        foreach (var type in SpellGroup(GetMagicFocus(pet)))
        {
            yield return (type, GetSpellCircle(type, pet) <= Math.Min(8, circleLimit));
        }
    }

    private static readonly Dictionary<(Mobile, HerdingSpecialization), double> SpecValue = new();

    public const double SpecMaxValue = 100.0;
    private const double SpecGainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetSpecValue(Mobile m, HerdingSpecialization spec) =>
        SpecValue.GetValueOrDefault((m, spec), 0.0);

    private static void TrainSpecialization(Mobile trainer, HerdingSpecialization spec)
    {
        var key = (trainer, spec);
        var current = SpecValue.GetValueOrDefault(key, 0.0);

        if (current < SpecMaxValue && Utility.RandomDouble() <= SpecGainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(SpecMaxValue, current + gain);
            SpecValue[key] = newValue;
            MahaonSkillTree.NotifyChanged(trainer);
            MahaonSkillTree.AnnounceGain(trainer, RuSpecializationName(spec), newValue - current, newValue);
        }
    }

    private static double GetSpecSpeedMultiplier(Mobile trainer, HerdingSpecialization spec)
    {
        var value = GetSpecValue(trainer, spec);

        var breadthTotal = 0.0;

        for (var s = 0; s < 4; s++)
        {
            var other = (HerdingSpecialization)s;

            if (other == spec)
            {
                continue;
            }

            breadthTotal += GetSpecValue(trainer, other);
        }

        var breadth = Math.Min(BreadthCap, breadthTotal / BreadthUnitsPerPercent);

        // Up to +50% tick speed (via smaller interval, applied at start) at 100 trained, plus breadth.
        return 1.0 + (value / 100.0 * 0.50 + breadth / 100.0);
    }

    private static double GetMaterialExpMultiplier(BaseStaff crook)
    {
        // Same tier split GatheringSpecializationSystem already uses for wood — reusing
        // the real vanilla CraftResource tiers, not inventing a new material scale.
        return crook.Resource switch
        {
            CraftResource.RegularWood => 1.0,
            CraftResource.OakWood     => 1.15,
            CraftResource.AshWood     => 1.30,
            CraftResource.YewWood     => 1.45,
            CraftResource.Heartwood   => 1.65,
            CraftResource.Bloodwood   => 1.85,
            CraftResource.Frostwood   => 2.0,
            _                          => 1.0
        };
    }

    public static bool CanTrainMagic(Mobile trainer) => trainer.Skills[SkillName.Magery].Value > 0;

    public static bool IsTraining(BaseCreature pet) => Active.ContainsKey(pet);

    public static void StopTraining(BaseCreature pet, string reasonMessage = null)
    {
        if (Active.Remove(pet, out var state))
        {
            state.timer.Stop();

            if (reasonMessage != null)
            {
                state.trainer.SendMessage(0x22, reasonMessage);
            }
        }
    }

    public static void StartTraining(
        BaseCreature pet, Mobile trainer, AnimalTrainingCategory category, BaseStaff crook, MagicFocus? focus = null
    )
    {
        if (IsTraining(pet))
        {
            StopTraining(pet);
        }

        if (category == AnimalTrainingCategory.Magic)
        {
            // Keeps whatever focus was already picked if the trainer is just resuming/
            // re-confirming the same category without going through the focus gump again;
            // MagicFocusGump always passes an explicit focus when the trainer actually
            // switches groups.
            PetMagicFocus[pet] = focus ?? GetMagicFocus(pet);
        }

        var spec = (HerdingSpecialization)category;
        var speedMult = GetSpecSpeedMultiplier(trainer, spec);
        var materialMult = GetMaterialExpMultiplier(crook);

        var expPerTick = 0.4 * materialMult; // base exp per tick before speed scaling
        // Was Math.Max(6.0, 12.0 / speedMult) — base interval at 6-12s. Shard owner asked
        // for a flat 2s tick; keeping the same proportional structure (speed specialization
        // still shortens it further, same ratio as before) rather than dropping training
        // speed-up entirely.
        var interval = TimeSpan.FromSeconds(Math.Max(1.0, 2.0 / speedMult));

        var timer = Timer.DelayCall(interval, interval, () => Tick(pet));
        Active[pet] = (category, trainer, expPerTick, timer);

        var focusNote = category == AnimalTrainingCategory.Magic
            ? $" ({RuFocusName(GetMagicFocus(pet))})"
            : "";

        trainer.SendMessage(
            0x59, $"{pet.Name} начинает тренировку ({RuCategoryName(category)}){focusNote}. Питомцу нужна еда поблизости."
        );
    }

    public static string RuFocusName(MagicFocus focus) => focus switch
    {
        MagicFocus.Support => "Поддержка",
        MagicFocus.Debuff  => "Дебафы",
        MagicFocus.Damage  => "Урон",
        MagicFocus.Summon  => "Призыв",
        _                  => focus.ToString()
    };

    private static readonly int[] OverheadHues = { 0x21, 0x35, 0x47, 0x59, 0x66, 0x7D, 0x8A, 0x98 };

    // Pets trained on land inside a fenced house territory (MahaonHouseFenceSystem) learn
    // faster — a real, tangible payoff for the fence, not just decoration.
    private static double EffectiveExpPerTick(BaseCreature pet, double baseExpPerTick) =>
        baseExpPerTick * Systems.MahaonWorld.MahaonHouseFenceSystem.GetTrainingBonusMultiplier(pet.Location, pet.Map);

    private static void Tick(BaseCreature pet)
    {
        if (pet.Deleted || !pet.Alive || !Active.TryGetValue(pet, out var state))
        {
            StopTraining(pet);
            return;
        }

        if (!ConsumeNearbyFood(pet, state.trainer))
        {
            StopTraining(pet, $"{pet.Name} не может продолжить тренировку — рядом нет еды.");
            return;
        }

        var done = state.category switch
        {
            AnimalTrainingCategory.Resistances  => TickResistances(pet, EffectiveExpPerTick(pet, state.expPerTick)),
            AnimalTrainingCategory.CombatSkills => TickCombatSkills(pet, EffectiveExpPerTick(pet, state.expPerTick)),
            AnimalTrainingCategory.Stats        => TickStats(pet, EffectiveExpPerTick(pet, state.expPerTick)),
            AnimalTrainingCategory.Magic        => TickMagic(pet, EffectiveExpPerTick(pet, state.expPerTick)),
            _                                     => true
        };

        TrainSpecialization(state.trainer, (HerdingSpecialization)state.category);

        var hue = OverheadHues[Utility.Random(OverheadHues.Length)];
        pet.PublicOverheadMessage(MessageType.Emote, hue, false, "*обучается*");

        if (done)
        {
            StopTraining(pet, $"{pet.Name} освоил всё, что можно было в этой области.");
        }
    }

    private static bool ConsumeNearbyFood(BaseCreature pet, Mobile trainer)
    {
        var map = pet.Map;

        if (map == null)
        {
            return false;
        }

        foreach (var item in map.GetItemsInRange(pet.Location, 3))
        {
            // Food and CookableFood are sibling classes (both : Item, neither inherits the
            // other) — RawRibs/RawLambLeg/RawFishSteak/RawBird/RawChickenLeg are all
            // CookableFood, so an `is Food` check alone silently rejected every raw meat,
            // only ever accepting already-cooked food as valid training food.
            if (item is (Food or CookableFood) && item.Amount > 0)
            {
                // Was read for the food-economy roll below but never actually GAINED
                // anywhere — CanTrainMagic/ConsumeNearbyFood only ever checked the
                // trainer's existing Herding value, no code path ever called CheckSkill for
                // it, so it sat permanently stuck wherever it started regardless of how
                // long the trainer kept feeding pets. Managing the food right here, every
                // tick training is active, is the natural moment to actually practice it.
                trainer.CheckSkill(SkillName.Herding, 0, 100);

                // Экономия еды — real Herding skill (not a separate tracked value) gives a
                // chance to skip consuming this tick's food unit entirely. Up to 30% at
                // 100 Herding — genuine reason to actually train the vanilla skill beyond
                // being a bare prerequisite for using the crook at all.
                var economyChance = trainer.Skills[SkillName.Herding].Value / 100.0 * 0.30;

                if (Utility.RandomDouble() < economyChance)
                {
                    return true; // "consumed" without actually touching the food
                }

                item.Consume();
                return true;
            }
        }

        return false;
    }

    private static bool TickResistances(BaseCreature pet, double exp)
    {
        var values = new[]
        {
            pet.PhysicalResistanceSeed, pet.FireResistSeed, pet.ColdResistSeed, pet.PoisonResistSeed, pet.EnergyResistSeed
        };

        var candidates = new List<int>();

        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] < 70) // reasonable ceiling — matches typical high-tier creature resist values
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return true; // fully trained
        }

        var pick = candidates[Utility.Random(candidates.Count)];
        var gain = Math.Max(1, (int)exp);

        switch (pick)
        {
            case 0:
                pet.PhysicalResistanceSeed += gain;
                break;
            case 1:
                pet.FireResistSeed += gain;
                break;
            case 2:
                pet.ColdResistSeed += gain;
                break;
            case 3:
                pet.PoisonResistSeed += gain;
                break;
            case 4:
                pet.EnergyResistSeed += gain;
                break;
        }

        return false;
    }

    private static readonly SkillName[] CombatSkillSet = { SkillName.Wrestling, SkillName.Tactics, SkillName.Anatomy, SkillName.Parry };

    // Damage moved here from TickStats (Str) per the shard owner's own call — "качается
    // через боевые" (trained through combat techniques instead). Flat cap like the
    // HP/Stam/Mana seeds, not per-species relative — 3000 total damage is an enormous
    // ceiling for any creature regardless of starting point, same reasoning as StatSeedCap.
    private const int DamageCap = 3000;
    private const double DamageGainChance = 0.3; // only some combat-skill ticks also nudge damage

    private static bool TickCombatSkills(BaseCreature pet, double exp)
    {
        var candidates = new List<SkillName>();

        foreach (var skill in CombatSkillSet)
        {
            if (pet.Skills[skill].Base < pet.Skills[skill].Cap)
            {
                candidates.Add(skill);
            }
        }

        var skillsDone = candidates.Count == 0;

        if (!skillsDone)
        {
            var pick = candidates[Utility.Random(candidates.Count)];
            pet.Skills[pick].Base = Math.Min(pet.Skills[pick].Cap, pet.Skills[pick].Base + exp);
        }

        var damageDone = pet.DamageMax >= DamageCap;

        if (!damageDone && Utility.RandomDouble() < DamageGainChance * GrowthSpeedFactor(pet.DamageMax, DamageCap))
        {
            pet.DamageMin += 1;
            pet.DamageMax += 1;
        }

        return skillsDone && (damageDone || pet.DamageMax >= DamageCap);
    }

    // Was RawStr/RawDex/RawInt — turns out every creature template sets HitsMaxSeed/
    // StamMaxSeed/ManaMaxSeed/DamageMin/Max explicitly via SetHits/SetStam/SetMana/
    // SetDamage at spawn (confirmed: only 4 files in the whole tree ever leave a Seed at
    // its -1 sentinel, meaning HitsMax = Str directly — everyone else is locked to a fixed
    // template value, completely decoupled from RawStr). Training raw stats was real and
    // persisted correctly, it just fed values nothing reads. Retargeted straight at the
    // actual seeds/damage so training visibly changes HP/stamina/mana/damage.
    //
    // Flat absolute cap (shard owner's own call: "чем больше тем медленнее, лимит
    // допустим 10к для статов") rather than a per-species relative one — simpler, and a
    // rabbit reaching the same numeric ceiling as a dragon is fine since getting anywhere
    // near 10000 at ~1 HP/tick is already an enormous, session-spanning grind on its own.
    // Growth SPEED itself tapers in stages as the seed approaches the cap (mirrors
    // TrainSpecialization's own diminishing-returns curve) instead of running at full
    // speed right up to a hard wall — a tick can still "fail" to land near the cap (no
    // stat change that tick, food is still consumed, training keeps going) rather than
    // the gain amount shrinking, since gains are already ~1/tick and there's no clean way
    // to shrink below that.
    private const int StatSeedCap = 10000;

    private static double GrowthSpeedFactor(int current, int cap)
    {
        var frac = (double)current / cap;

        return frac switch
        {
            < 0.3  => 1.0,
            < 0.6  => 0.5,
            < 0.85 => 0.25,
            _      => 0.1
        };
    }

    /// <summary>For UI (MahaonAnimalLoreGump) — (current, cap) for HP/Stamina/Mana/Damage.
    /// Damage is trained via TickCombatSkills now, not here — see DamageCap there.</summary>
    public static (int hits, int hitsCap, int stam, int stamCap, int mana, int manaCap, int dmgMax, int dmgMaxCap)
        GetPhysicalCaps(BaseCreature pet)
    {
        return (
            pet.HitsMaxSeed, StatSeedCap,
            pet.StamMaxSeed, StatSeedCap,
            pet.ManaMaxSeed, StatSeedCap,
            pet.DamageMax, DamageCap
        );
    }

    private static bool TickStats(BaseCreature pet, double exp)
    {
        var gain = Math.Max(1, (int)exp);

        var candidates = new List<int>();

        if (pet.HitsMaxSeed < StatSeedCap)
        {
            candidates.Add(0); // "Сила" — HP
        }

        if (pet.StamMaxSeed < StatSeedCap)
        {
            candidates.Add(1); // "Ловкость" — Stamina
        }

        if (pet.ManaMaxSeed < StatSeedCap)
        {
            candidates.Add(2); // "Интеллект" — Mana
        }

        if (candidates.Count == 0)
        {
            return true; // fully trained
        }

        var picked = candidates[Utility.Random(candidates.Count)];

        switch (picked)
        {
            case 0:
                if (Utility.RandomDouble() < GrowthSpeedFactor(pet.HitsMaxSeed, StatSeedCap))
                {
                    pet.HitsMaxSeed += gain;
                    pet.Hits += gain;
                }

                break;
            case 1:
                if (Utility.RandomDouble() < GrowthSpeedFactor(pet.StamMaxSeed, StatSeedCap))
                {
                    pet.StamMaxSeed += gain;
                    pet.Stam += gain;
                }

                break;
            case 2:
                if (Utility.RandomDouble() < GrowthSpeedFactor(pet.ManaMaxSeed, StatSeedCap))
                {
                    pet.ManaMaxSeed += gain;
                    pet.Mana += gain;
                }
                break;
        }

        return false;
    }

    private static bool TickMagic(BaseCreature pet, double exp)
    {
        MagicTrainedPets.Add(pet);
        EnsureSpellcasterTimer(pet);

        // EvalInt rides along with Magery — it's Spell.DamageSkill for every Magery spell
        // (GetNewAosDamage reads Caster.Int and the caster's own damage-scaling formula,
        // same path a real player mage's EvalInt training benefits from) — this is what
        // makes "training increases cast power" true without any custom per-spell bonus:
        // it's the exact same vanilla formula everyone else's magic damage already uses.
        var mageryDone = pet.Skills[SkillName.Magery].Base >= pet.Skills[SkillName.Magery].Cap;
        var evalIntDone = pet.Skills[SkillName.EvalInt].Base >= pet.Skills[SkillName.EvalInt].Cap;

        if (!mageryDone)
        {
            pet.Skills[SkillName.Magery].Base = Math.Min(
                pet.Skills[SkillName.Magery].Cap, pet.Skills[SkillName.Magery].Base + exp
            );
        }

        if (!evalIntDone)
        {
            pet.Skills[SkillName.EvalInt].Base = Math.Min(
                pet.Skills[SkillName.EvalInt].Cap, pet.Skills[SkillName.EvalInt].Base + exp
            );
        }

        return mageryDone && evalIntDone;
    }

    private static readonly HashSet<BaseCreature> SpellcasterTimerStarted = new();
    private static readonly Dictionary<Mobile, DateTime> LastBuffedTime = new();

    /// <summary>Called from the shared Spell.cs cast-completion hook, right after
    /// OnCast() sets Caster.Target — for a player this waits for a mouse click, but
    /// nothing auto-resolves it for a non-player caster (checked directly: the vanilla
    /// pipeline only handles the caster.Player branch there). Harmful spells target the
    /// pet's current Combatant, beneficial ones target the pet's owner — decided from
    /// Target.Flags, not by guessing the spell's identity. Anything with neither flag
    /// reaching this point is a Summon-group spell (location target, TargetFlags.None) —
    /// safe to assume since CastRandom only ever picks from our own curated groups, so no
    /// other flagless Magery spell can end up here.</summary>
    public static void TryAutoResolveTarget(BaseCreature caster)
    {
        if (!IsMagicTrained(caster) || caster.Target == null)
        {
            return;
        }

        if ((caster.Target.Flags & TargetFlags.Harmful) != 0)
        {
            if (caster.Combatant != null && caster.Combatant.Alive)
            {
                caster.Target.Invoke(caster, caster.Combatant);
            }
            else
            {
                caster.Target.Cancel(caster, TargetCancelType.Canceled);
            }
        }
        else if ((caster.Target.Flags & TargetFlags.Beneficial) != 0)
        {
            var beneficiary = caster.ControlMaster?.Alive == true ? caster.ControlMaster : caster;
            caster.Target.Invoke(caster, beneficiary);
        }
        else
        {
            caster.Target.Invoke(caster, caster.Location);
        }
    }

    /// <summary>Started once, the first time a pet is magic-trained — persists even
    /// after training itself stops, since the pet has permanently learned to cast (same
    /// framing as a player who trained a skill and keeps it). Every few seconds, checks
    /// for a debuff-worthy enemy or a buff-worthy owner and initiates a cast — the actual
    /// targeting is handled by TryAutoResolveTarget above once the cast delay completes.</summary>
    private static void EnsureSpellcasterTimer(BaseCreature pet)
    {
        if (!SpellcasterTimerStarted.Add(pet))
        {
            return;
        }

        Timer.DelayCall(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(6), () => SpellcasterCheck(pet));
    }

    private static readonly Dictionary<BaseCreature, DateTime> LastSummonTime = new();
    private static readonly Dictionary<BaseCreature, DateTime> LastDamageCastTime = new();

    /// <summary>Only ever casts from the pet's own chosen MagicFocus group now — was a
    /// fixed 50/50 debuff-enemy/buff-owner split with no Damage or Summon path at all, so
    /// picking "Урон" as a focus previously did nothing extra in combat beyond the pet's
    /// normal melee (Magery just sat there as an unused number). Each focus has its own
    /// targeting/cooldown shape since the four groups behave nothing alike.</summary>
    private static void SpellcasterCheck(BaseCreature pet)
    {
        if (pet.Deleted || !pet.Alive || !IsMagicTrained(pet))
        {
            SpellcasterTimerStarted.Remove(pet);
            return;
        }

        if (pet.Spell != null) // already mid-cast
        {
            return;
        }

        var magery = pet.Skills[SkillName.Magery].Value;

        if (magery <= 0)
        {
            return;
        }

        var circleLimit = (int)(magery / 10) + 1;
        var focus = GetMagicFocus(pet);

        switch (focus)
        {
            case MagicFocus.Debuff:
            case MagicFocus.Damage:
                if (pet.Combatant?.Alive == true && pet.InRange(pet.Combatant, 10))
                {
                    // Damage spells have their own real cast delay/mana cost already
                    // slowing them down, but a short extra cooldown keeps a fast-ticking
                    // SpellcasterCheck (every 6s) from spamming casts back to back the
                    // instant a fight starts.
                    var lastCast = LastDamageCastTime.GetValueOrDefault(pet, DateTime.MinValue);

                    if (Core.Now - lastCast > TimeSpan.FromSeconds(4))
                    {
                        LastDamageCastTime[pet] = Core.Now;
                        CastRandom(pet, SpellGroup(focus), circleLimit);
                    }
                }

                break;

            case MagicFocus.Support:
                var master = pet.ControlMaster;

                if (master?.Alive == true && pet.InRange(master, 10))
                {
                    var lastBuff = LastBuffedTime.GetValueOrDefault(master, DateTime.MinValue);

                    if (Core.Now - lastBuff > TimeSpan.FromMinutes(2))
                    {
                        LastBuffedTime[master] = Core.Now;
                        CastRandom(pet, SpellGroup(focus), circleLimit);
                    }
                }

                break;

            case MagicFocus.Summon:
                var lastSummon = LastSummonTime.GetValueOrDefault(pet, DateTime.MinValue);

                // Only bother summoning help while there's actually something to fight —
                // an idle pet doesn't need a standing army around it.
                if (pet.Combatant?.Alive == true && Core.Now - lastSummon > TimeSpan.FromMinutes(3))
                {
                    LastSummonTime[pet] = Core.Now;
                    CastRandom(pet, SpellGroup(focus), circleLimit);
                }

                break;
        }
    }

    private static void CastRandom(BaseCreature pet, Type[] spells, int circleLimit)
    {
        var castable = spells.Where(t => GetSpellCircle(t, pet) <= Math.Min(8, circleLimit)).ToArray();

        if (castable.Length == 0)
        {
            return; // not skilled enough yet for anything in this group — try again next tick
        }

        var pick = castable[Utility.Random(castable.Length)];
        var spell = (MagerySpell)Activator.CreateInstance(pick, pet, null);

        spell.Cast();
    }

    public static string RuCategoryName(AnimalTrainingCategory category) => category switch
    {
        AnimalTrainingCategory.Resistances  => "Сопротивления",
        AnimalTrainingCategory.CombatSkills => "Боевые техники",
        AnimalTrainingCategory.Stats        => "Физическая подготовка",
        AnimalTrainingCategory.Magic        => "Магия",
        _                                     => category.ToString()
    };

    public static string RuSpecializationName(HerdingSpecialization spec) => spec switch
    {
        HerdingSpecialization.Resistances  => "Тренер сопротивлений",
        HerdingSpecialization.CombatSkills => "Тренер боевых техник",
        HerdingSpecialization.Stats        => "Тренер физподготовки",
        HerdingSpecialization.Magic        => "Тренер магии",
        _                                    => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonHerdingSpecialization", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            MahaonSpecializationPersistenceHelper.Write(writer, SpecValue, (w, key) => w.Write((byte)key));
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            MahaonSpecializationPersistenceHelper.Read(reader, SpecValue, r => (HerdingSpecialization)r.ReadByte());
        }
    }
}
