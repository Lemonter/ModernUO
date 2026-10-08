using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonMasteries;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Real OSI/ServUO's DamageType enum (Melee/Spell/SpecialAbility/etc, used throughout their
// AOS.Damage pipeline) doesn't exist anywhere in this codebase — MahaonCombat's own damage
// pipeline doesn't distinguish by this axis at all. Rather than wire a whole new concept
// into the engine's damage path for masteries alone, this is a small enum scoped to just
// the mastery hooks that want to tell "what kind of hit was this" apart (a few
// OnDamaged/OnTargetDamaged overrides check it) — Melee/Ranged pass through OnHit/OnMiss
// with real data, Spell/Other are set explicitly by whichever mastery ability deals that
// kind of damage.
public enum DamageType
{
    Melee,
    Ranged,
    Spell,
    Other,

    // Present in the original enum; Spellweaving's Wildfire classifies its ticks with it.
    SpellAOE
}

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Core/
// SkillMasterySpell.cs, ~1300 lines). Simplified relative to the original in one real way:
// party-wide effect sharing (PartyList/AddPartyMember/CheckPartyEffects/the whole
// UpdateParty machinery) is dropped — every ability here affects only its own caster (and,
// where the original explicitly targets one ally, that one target). Mahaon bots are mostly
// solo/duo, and full N-player party broadcast is a large, separate feature; PartyEffects/
// PartyRange stay as declared virtuals so a real party-sharing pass can slot in later
// without touching every ability file again. Static dispatcher methods (OnHit/OnMiss/
// OnParried/OnDamage/OnWeaponRemoved) are unchanged in shape from the original — they ARE
// wired in as the actual integration point, called from BaseWeapon.cs (OnHit before
// AOS.Damage, OnMiss alongside the existing WeaponAbility/SpecialMove OnMiss calls,
// OnParried inside AbsorbDamageAOS's blocked branch, OnWeaponRemoved from Item.OnRemoved —
// same additive pattern as every other MahaonCombat hook already there) and from
// AOS.Damage/SpellHelper's typed-damage overload in AOS.cs/SpellHelper.cs, which now also
// carry an optional DamageType parameter (Melee/Ranged from BaseWeapon, Spell from
// SpellHelper, default Other elsewhere) so abilities like BodyGuardSpell can tell weapon
// hits from spell damage. SkillMasteryMove (the WeaponAbility/SpecialMove-shaped half of
// this port — StaggerMove, OnslaughtMove, PierceMove, FistsOfFuryMove) needed no wiring at
// all: it rides the engine's existing SpecialMove.GetCurrentMove() dispatch untouched.
public abstract class SkillMasterySpell : Spell
{
    public static void Configure() =>
        CommandSystem.Register("LearnAllMasteries", AccessLevel.GameMaster, LearnAllMasteries_OnCommand);

    [Usage("LearnAllMasteries")]
    [Description("Grants the targeted mobile Volume III in every mastery skill, for testing.")]
    private static void LearnAllMasteries_OnCommand(CommandEventArgs e)
    {
        e.Mobile.Target = new LearnAllTarget();
    }

    private class LearnAllTarget : Target
    {
        public LearnAllTarget() : base(-1, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile m)
            {
                return;
            }

            foreach (var skill in MasteryInfo.Skills)
            {
                MasteryState.LearnMastery(m, skill, 3);
            }

            from.SendMessage("Granted Volume III in every mastery skill.");
        }
    }

    public UpkeepTimer Timer { get; set; }
    public Mobile Target { get; set; }
    public DateTime Expires { get; set; }

    public virtual double RequiredSkill => 90.0;
    public virtual double UpKeep => 0;
    public virtual int RequiredMana => 10;
    public virtual bool PartyEffects => false;
    public virtual int PartyRange => 12;
    public virtual int DamageThreshold => 100;
    public virtual bool DamageCanDisrupt => false;
    public virtual double TickTime => 2;

    public virtual int UpkeepCancelMessage => 1156111; // You do not have enough mana to keep your ability active.
    public virtual int OutOfRangeMessage => 1156098;    // Your target is no longer in range of your ability.
    public virtual int DisruptMessage => 1156110;       // Your ability was canceled.
    public virtual int ExpireMessage => 0;

    public virtual bool CancelsWeaponAbility => false;
    public virtual bool CancelsSpecialMove => CancelsWeaponAbility;
    public virtual bool RevealOnTick => true;

    public virtual TimeSpan ExpirationPeriod => TimeSpan.FromMinutes(30);
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.25);

    public virtual double BaseSkillBonus =>
        Caster == null ? 0.0 : (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value + GetMasteryLevel() * 40) / 3;

    public override bool ClearHandsOnCast => false;
    public override bool BlocksMovement => true;

    protected SkillMasterySpell(Mobile caster, Item scroll, SpellInfo info) : base(caster, scroll, info)
    {
    }

    public override bool CheckCast()
    {
        var mana = ScaleMana(RequiredMana);

        if (!base.CheckCast())
        {
            return false;
        }

        if (IsInCooldown(Caster, GetType()))
        {
            return false;
        }

        if (Caster.Player && Caster.Skills[CastSkill].Value < RequiredSkill)
        {
            Caster.SendLocalizedMessage(1115709); // Your skills are not high enough to invoke this mastery ability.
        }
        else if (Caster is PlayerMobile && MasteryState.GetCurrentMastery(Caster) != CastSkill)
        {
            Caster.SendLocalizedMessage(1115664); // You are not on the correct path for using this mastery ability.
        }
        else if (Caster is PlayerMobile && !MasteryInfo.HasLearned(Caster, CastSkill))
        {
            Caster.SendLocalizedMessage(1115664);
        }
        else if (Caster.Mana < mana)
        {
            Caster.SendLocalizedMessage(1060174, mana.ToString()); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
        }
        else
        {
            if (CancelsWeaponAbility)
            {
                WeaponAbility.ClearCurrentAbility(Caster);
            }

            if (CancelsSpecialMove)
            {
                SpecialMove.ClearCurrentMove(Caster);
            }

            return true;
        }

        return false;
    }

    public override void DoFizzle()
    {
        Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.

        if (Caster.Player)
        {
            Caster.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
        }
    }

    public override void GetCastSkills(out double min, out double max)
    {
        min = RequiredSkill;
        max = RequiredSkill + 25.0;
    }

    public override int GetMana() => RequiredMana;

    public BaseWeapon GetWeapon() => Caster.Weapon as BaseWeapon;

    public bool CheckWeapon()
    {
        if (!Caster.Player)
        {
            return true;
        }

        var weapon = GetWeapon();

        if (CastSkill == SkillName.Poisoning && weapon != null && weapon is not Fists)
        {
            return true;
        }

        return weapon != null && weapon.DefSkill == CastSkill;
    }

    public virtual bool OnTick()
    {
        if (RevealOnTick)
        {
            Caster.RevealingAction();
        }

        var upkeep = ScaleUpkeep();

        if ((Caster is PlayerMobile && Caster.NetState == null) || Expires < Core.Now || !Caster.Alive || Caster.IsDeadBondedPet)
        {
            Expire();
        }
        else if (Target != null && !Target.Alive)
        {
            Expire();
        }
        else if (Target != null && !Caster.InRange(Target.Location, PartyRange))
        {
            Expire();

            if (OutOfRangeMessage > 0)
            {
                Caster.SendLocalizedMessage(OutOfRangeMessage);
            }
        }
        else if (Caster.Mana < upkeep)
        {
            if (UpkeepCancelMessage > 0)
            {
                Caster.SendLocalizedMessage(UpkeepCancelMessage);
            }

            Expire();
        }
        else if (Caster.Player && Caster.Skills[CastSkill].Value < RequiredSkill)
        {
            Expire();
        }
        else
        {
            DoEffects();

            if (upkeep > 0)
            {
                Caster.Mana -= upkeep;
            }

            return true;
        }

        return false;
    }

    protected virtual void DoEffects()
    {
    }

    /// <summary>Hook for the single-target mastery spells that cast via Caster.Target =
    /// new MasteryTarget(this, ...) instead of an immediate CheckSequence in OnCast (real
    /// ServUO shape, kept as-is — DeathRay/CommandUndead/Conduit all target something
    /// first, then resolve).</summary>
    protected virtual void OnTarget(object targeted)
    {
    }

    public void InvokeOnTarget(object targeted) => OnTarget(targeted);

    // Bard spells (InspireSpell etc.) read these back through their own AOS-pipeline hooks
    // (HCI/SDI/damage/regen bonuses) — kept as plain virtuals here rather than only on
    // BardSpell since a couple of non-bard spells override PropertyBonus too.
    public virtual int PropertyBonus() => 0;
    public virtual int PropertyBonus2() => 0;
    public virtual int StatBonus() => 0;
    public virtual int DamageBonus() => 0;

    public virtual void AddStatMods()
    {
    }

    public virtual void RemoveStatMods()
    {
    }

    public virtual void EndEffects()
    {
    }

    public virtual void OnDamaged(Mobile attacker, Mobile defender, DamageType type, ref int damage)
    {
    }

    public virtual void OnHit(Mobile defender, ref int damage)
    {
    }

    public virtual void OnGotHit(Mobile attacker, ref int damage)
    {
    }

    public virtual void OnMiss(Mobile defender)
    {
    }

    public virtual void OnGotMiss(Mobile attacker)
    {
    }

    public virtual void OnParried(Mobile attacker)
    {
    }

    public virtual void OnGotParried(Mobile defender)
    {
    }

    public virtual void OnTargetDamaged(Mobile attacker, Mobile victim, DamageType type, ref int damage)
    {
    }

    public virtual void OnWeaponRemoved(BaseWeapon weapon)
    {
    }

    public virtual int ScaleUpkeep() => UpKeep == 0 ? 0 : ScaleMana((int)UpKeep);

    public virtual void Expire(bool disrupt = false)
    {
        if (Timer != null)
        {
            if (ExpireMessage > 0)
            {
                Caster.SendLocalizedMessage(ExpireMessage);
            }

            Timer.Stop();
            Timer = null;
        }

        if (disrupt && DisruptMessage > 0)
        {
            Caster.SendLocalizedMessage(DisruptMessage);
        }

        Server.Timer.DelayCall(RemoveFromTable);
        RemoveStatMods();
        EndEffects();

        OnExpire();
    }

    public virtual void OnExpire()
    {
    }

    public virtual double DamageModifier(Mobile victim)
    {
        var dSkill = Caster.Skills[DamageSkill].Value;
        var vSkill = GetResistSkill(victim);

        var reduce = 1.0 - (dSkill - vSkill) / dSkill;
        return Math.Clamp(reduce, 0.0, 1.0);
    }

    public virtual bool CheckResisted(Mobile target)
    {
        var volumeMod = GetMasteryLevel() * 2;
        var n = GetResistPercent(target, volumeMod) / 100.0;

        if (n <= 0.0)
        {
            return false;
        }

        if (n >= 1.0)
        {
            return true;
        }

        var maxSkill = (1 + volumeMod) * 10 + (1 + volumeMod / 6) * 25;

        if (target.Skills[SkillName.MagicResist].Value < maxSkill)
        {
            target.CheckSkill(SkillName.MagicResist, 0.0, target.Skills[SkillName.MagicResist].Cap);
        }

        return n >= Utility.RandomDouble();
    }

    public virtual double GetResistPercent(Mobile target, int level)
    {
        var value = GetResistSkill(target);
        var firstPercent = value / 5.0;
        var secondPercent = value - ((Caster.Skills[CastSkill].Value - 20.0) / 5.0 + (1 + level) * 5.0);

        return Math.Max(firstPercent, secondPercent) / 2.0;
    }

    protected virtual double GetResistSkill(Mobile target) => target.Skills[SkillName.MagicResist].Value;

    public int GetMasteryLevel() => Math.Max(Caster is BaseCreature ? 1 : 0, MasteryInfo.GetMasteryLevel(Caster, CastSkill));

    private static readonly Dictionary<Mobile, List<SkillMasterySpell>> Table = new();
    private static readonly object Lock = new();

    public static IEnumerable<SkillMasterySpell> EnumerateAllSpells()
    {
        List<SkillMasterySpell> list;

        lock (Lock)
        {
            list = Table.Values.SelectMany(v => v).ToList();
        }

        return list;
    }

    public static IEnumerable<SkillMasterySpell> EnumerateSpells(Mobile from, Type t = null)
    {
        List<SkillMasterySpell> list;

        lock (Lock)
        {
            if (!Table.TryGetValue(from, out list) || list.Count == 0)
            {
                return Enumerable.Empty<SkillMasterySpell>();
            }

            list = new List<SkillMasterySpell>(list);
        }

        return t == null ? list : list.Where(s => s.GetType() == t);
    }

    public static List<SkillMasterySpell> GetSpells(Mobile m)
    {
        lock (Lock)
        {
            return Table.TryGetValue(m, out var list) ? new List<SkillMasterySpell>(list) : new List<SkillMasterySpell>();
        }
    }

    public static SkillMasterySpell GetSpell(Mobile from, Type type) => EnumerateSpells(from, type).FirstOrDefault();

    public static TSpell GetSpell<TSpell>(Mobile m) where TSpell : SkillMasterySpell =>
        EnumerateSpells(m, typeof(TSpell)).FirstOrDefault() as TSpell;

    public static TSpell GetSpell<TSpell>(Mobile caster, Mobile target) where TSpell : SkillMasterySpell =>
        EnumerateSpells(caster, typeof(TSpell)).FirstOrDefault(s => s.Target == target) as TSpell;

    public static bool HasSpell(Mobile from, Type type) => EnumerateSpells(from, type).Any();

    /// <summary>Whether ANY active caster's spell of this type currently has `from` as its
    /// Target — used by the harmful-targeted bard spells (Tribulation/Despair) to refuse
    /// double-stacking their debuff on the same victim from a different caster.</summary>
    public static bool HasHarmfulEffects(Mobile target, Type type) => EnumerateAllSpells().Any(s => s.GetType() == type && s.Target == target);

    public static bool HasSpell<TSpell>(Mobile from) where TSpell : SkillMasterySpell => EnumerateSpells(from, typeof(TSpell)).Any();

    protected void AddToTable(Mobile from, SkillMasterySpell spell)
    {
        lock (Lock)
        {
            if (!Table.TryGetValue(from, out var list))
            {
                list = new List<SkillMasterySpell>();
                Table[from] = list;
            }

            if (!list.Contains(spell))
            {
                list.Add(spell);
            }
        }
    }

    protected void RemoveFromTable()
    {
        lock (Lock)
        {
            if (Table.TryGetValue(Caster, out var list) && list.Remove(this) && list.Count == 0)
            {
                Table.Remove(Caster);
            }
        }

        Caster.Delta(MobileDelta.WeaponDamage);
        Target?.Delta(MobileDelta.WeaponDamage);
    }

    /// <summary>Hook point — called from AOS.Damage (Misc/AOS.cs), one additive line, same
    /// as the OnHit/OnMiss/OnParried hooks in BaseWeapon.cs below.</summary>
    public static void OnDamage(Mobile victim, Mobile damager, DamageType type, ref int damage)
    {
        if (victim == null || damager == null)
        {
            return;
        }

        foreach (var sp in EnumerateSpells(victim).ToList())
        {
            if (sp.DamageCanDisrupt && damage >= Utility.Random(sp.DamageThreshold))
            {
                sp.Expire(true);
            }

            sp.OnDamaged(damager, victim, type, ref damage);
        }

        if (SpecialMove.GetCurrentMove(victim) is SkillMasteryMove move)
        {
            move.OnDamaged(damager, victim, type, ref damage);
        }

        CombatTrainingSpell.CheckDamage(damager, victim, type, ref damage);
    }

    /// <summary>Hook point — called from BaseWeapon.OnHit, one additive line.</summary>
    public static void OnHit(Mobile attacker, Mobile defender, ref int damage)
    {
        if (attacker == null || defender == null)
        {
            return;
        }

        foreach (var spell in EnumerateSpells(attacker).ToList())
        {
            spell.OnHit(defender, ref damage);
        }

        foreach (var spell in EnumerateSpells(defender).ToList())
        {
            spell.OnGotHit(attacker, ref damage);
        }

        if (SpecialMove.GetCurrentMove(defender) is SkillMasteryMove move)
        {
            move.OnGotHit(attacker, defender, ref damage);
        }

        if (attacker is BaseCreature || defender is BaseCreature)
        {
            CombatTrainingSpell.OnCreatureHit(attacker, defender, ref damage);
        }
    }

    /// <summary>Hook point — called from BaseWeapon.OnMiss, one additive line.</summary>
    public static void OnMiss(Mobile attacker, Mobile defender)
    {
        if (attacker == null || defender == null)
        {
            return;
        }

        foreach (var spell in EnumerateSpells(attacker).ToList())
        {
            spell.OnMiss(defender);
        }

        foreach (var spell in EnumerateSpells(defender).ToList())
        {
            spell.OnGotMiss(attacker);
        }
    }

    /// <summary>Hook point — called from BaseWeapon's parry handling, one additive line.</summary>
    public static void OnParried(Mobile attacker, Mobile defender)
    {
        if (attacker == null || defender == null)
        {
            return;
        }

        foreach (var spell in EnumerateSpells(defender).ToList())
        {
            spell.OnParried(attacker);
        }

        foreach (var spell in EnumerateSpells(attacker).ToList())
        {
            spell.OnGotParried(defender);
        }
    }

    public static void OnWeaponRemoved(Mobile from, BaseWeapon weapon)
    {
        foreach (var spell in EnumerateSpells(from).ToList())
        {
            spell.OnWeaponRemoved(weapon);
        }
    }

    public static void CancelWeaponAbility(Mobile attacker)
    {
        foreach (var spell in EnumerateSpells(attacker).Where(s => s.CancelsWeaponAbility).ToList())
        {
            spell.Expire();
        }
    }

    public static void CancelSpecialMove(Mobile attacker)
    {
        foreach (var spell in EnumerateSpells(attacker).Where(s => s.CancelsSpecialMove).ToList())
        {
            spell.Expire();
        }
    }

    private static readonly Dictionary<(Type, Mobile), DateTime> Cooldowns = new();

    protected void AddToCooldown(TimeSpan ts)
    {
        Cooldowns[(GetType(), Caster)] = Core.Now + ts;
        Server.Timer.DelayCall(ts, () => Cooldowns.Remove((GetType(), Caster)));
    }

    public static bool IsInCooldown(Mobile m, Type type, bool message = true)
    {
        if (!Cooldowns.TryGetValue((type, m), out var until))
        {
            return false;
        }

        if (Core.Now >= until)
        {
            Cooldowns.Remove((type, m));
            return false;
        }

        if (message)
        {
            var left = (until - Core.Now).TotalSeconds;
            m.SendLocalizedMessage(1079335, left.ToString("F1")); // You must wait ~1_seconds~ seconds before you can use this ability again.
        }

        return true;
    }

    protected virtual void BeginTimer()
    {
        Timer?.Stop();

        Timer = new UpkeepTimer(this);
        Timer.Start();

        if (Expires < Core.Now)
        {
            Expires = Core.Now + ExpirationPeriod;
        }

        AddToTable(Caster, this);
        AddStatMods();

        if (RevealOnTick)
        {
            Caster.RevealingAction();
        }

        Caster.Delta(MobileDelta.WeaponDamage);
        Target?.Delta(MobileDelta.WeaponDamage);
    }

    public int GetWeaponSkill()
    {
        Span<SkillName> weaponSkills =
        [
            SkillName.Swords, SkillName.Fencing, SkillName.Macing,
            SkillName.Archery, SkillName.Throwing, SkillName.Wrestling
        ];

        var best = 0.0;

        foreach (var skill in weaponSkills)
        {
            best = Math.Max(best, Caster.Skills[skill].Value);
        }

        return (int)best;
    }

    public class UpkeepTimer : Timer
    {
        private readonly SkillMasterySpell _spell;

        public UpkeepTimer(SkillMasterySpell spell) : base(TimeSpan.FromSeconds(spell.TickTime), TimeSpan.FromSeconds(spell.TickTime)) =>
            _spell = spell;

        protected override void OnTick() => _spell.OnTick();
    }
}
