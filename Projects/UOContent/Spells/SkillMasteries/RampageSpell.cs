using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Rampage.cs) —
// Wrestling mastery: stacking regen/swing-speed buff that builds on every successful
// unarmed hit and resets on a miss or parry.
public class RampageSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Rampage", "", -1, 9002);

    public override int RequiredMana => 20;
    public override int DamageThreshold => 0;
    public override SkillName CastSkill => SkillName.Wrestling;

    public RampageSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        var weapon = GetWeapon();

        if (Caster.Player && weapon is not Fists)
        {
            Caster.SendLocalizedMessage(1155979); // You may not wield a weapon and use this ability.
            return false;
        }

        return base.CheckCast();
    }

    public override void OnBeginCast()
    {
        base.OnBeginCast();

        if (HasSpell(Caster, GetType()))
        {
            return;
        }

        Caster.PrivateOverheadMessage(MessageType.Regular, 1150, 1155890, Caster.NetState); // *You attempt channel your wrestling mastery into a fit of rage!*

        if (Caster.Player)
        {
            Caster.PlaySound(Caster.Female ? 0x338 : 0x44A);
        }
        else if (Caster is BaseCreature bc)
        {
            Caster.PlaySound(bc.GetAngerSound());
        }
    }

    public override void OnCast()
    {
        if (GetSpell(Caster, typeof(RampageSpell)) is RampageSpell spell)
        {
            spell.Expire();
        }
        else if (CheckSequence())
        {
            Effects.SendTargetParticles(Caster, 0x37CC, 1, 40, 2724, 5, 9907, EffectLayer.LeftFoot, 0);
            Caster.PlaySound(0x101);

            Expires = Core.Now + TimeSpan.FromSeconds(60);
            BeginTimer();

            AddToTable();
        }

        FinishSequence();
    }

    private void AddToTable()
    {
        var c = new RampageContext(this);
        _table[Caster] = c;

        RefreshBuff(c);
    }

    private void RefreshBuff(RampageContext c)
    {
        if (Caster is not PlayerMobile pm)
        {
            return;
        }

        pm.RemoveBuff(BuffIcon.Rampage);
        // Each successful hit grants +HP/Stam Regen/Swing Speed/Casting Focus; resets on a miss.
        pm.AddBuff(new BuffInfo(BuffIcon.Rampage, 1155929, 1155893, TimeSpan.FromSeconds(60),
            $"{1 + GetMasteryLevel()}\t{GetMasteryLevel()}\t{GetMasteryLevel()}\t{GetMasteryLevel()}\t{c.HitsRegen}\t{c.StamRegen}\t{c.SwingSpeed}\t{c.Focus}"));
    }

    public override void EndEffects() => RemoveFromTable(Caster);

    public override void OnGotParried(Mobile defender)
    {
        RemoveFromTable(Caster);
        Expire();
    }

    public override void OnMiss(Mobile defender)
    {
        RemoveFromTable(Caster);
        Expire();
    }

    public override void OnHit(Mobile defender, ref int damage)
    {
        if (!_table.TryGetValue(Caster, out var c))
        {
            return;
        }

        Caster.PlaySound(0x3B4);
        c.IncreaseBuffs();
        RefreshBuff(c);
    }

    public static bool RemoveFromTable(Mobile m)
    {
        if (!_table.Remove(m))
        {
            return false;
        }

        if (m is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Rampage);
        }

        return true;
    }

    public static int GetBonus(Mobile m, BonusType type)
    {
        if (!_table.TryGetValue(m, out var c))
        {
            return 0;
        }

        return type switch
        {
            BonusType.HitPointRegen => c.HitsRegen,
            BonusType.StamRegen     => c.StamRegen,
            BonusType.Focus         => c.Focus,
            BonusType.SwingSpeed    => c.SwingSpeed,
            _                       => 0
        };
    }

    private static readonly Dictionary<Mobile, RampageContext> _table = new();

    public enum BonusType
    {
        HitPointRegen,
        StamRegen,
        Focus,
        SwingSpeed
    }

    private class RampageContext
    {
        private readonly RampageSpell _spell;

        public int HitsRegen { get; private set; }
        public int StamRegen { get; private set; }
        public int Focus { get; private set; }
        public int SwingSpeed { get; private set; }

        private const int HitsMax = 18;
        private const int StamMax = 24;
        private const int SwingMax = 60;
        private const int FocusMax = 12;

        public RampageContext(RampageSpell spell) => _spell = spell;

        public void IncreaseBuffs()
        {
            HitsRegen = Math.Min(HitsMax, HitsRegen + 1 + _spell.GetMasteryLevel());
            StamRegen = Math.Min(StamMax, StamRegen + _spell.GetMasteryLevel());
            Focus = Math.Min(FocusMax, Focus + _spell.GetMasteryLevel());
            SwingSpeed = Math.Min(SwingMax, SwingSpeed + _spell.GetMasteryLevel());
        }
    }
}
