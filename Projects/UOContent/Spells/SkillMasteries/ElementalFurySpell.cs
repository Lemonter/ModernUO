using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/ElementalFury.cs) —
// Throwing mastery: builds up an elemental "Fury Pool" per-target on hit, unleashing a
// burst of matching-element damage once it fills.
public class ElementalFurySpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Elemental Fury", "", -1, 9002);

    public override double RequiredSkill => 90;
    public override int RequiredMana => 20;

    public override SkillName CastSkill => SkillName.Throwing;
    public override SkillName DamageSkill => SkillName.Tactics;

    private int _maxAdd;
    private ResistanceType _type;

    private Dictionary<Mobile, int> _table;

    public ElementalFurySpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (IsInCooldown(Caster, GetType()))
        {
            return false;
        }

        if (!CheckWeapon())
        {
            Caster.SendLocalizedMessage(1156016); // You must have a throwing weapon equipped to use this ability.
            return false;
        }

        if (GetSpell(Caster, GetType()) is ElementalFurySpell spell)
        {
            spell.Expire();
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
            Caster.PlaySound(0x101);

            Caster.PrivateOverheadMessage(MessageType.Regular, 1150, 1156017, Caster.NetState); // *Your throw is enhanced by the Elemental Fury!*

            var skill = BaseSkillBonus;

            var duration = TimeSpan.FromSeconds(skill);
            _maxAdd = (int)(skill / 10) + Utility.RandomMinMax(-1, 0);

            Expires = Core.Now + duration;
            BeginTimer();

            _type = GetResistanceType(GetWeapon());

            if (Caster is PlayerMobile pm)
            {
                // Each attack the caster deals with ~1_TYPE~ damage will add up to ~3_VAL~
                // damage to the Fury Pool. Once the Fury Pool reaches ~2_VAL~ the throwing
                // weapon will unleash the Elemental Fury.
                pm.AddBuff(new BuffInfo(BuffIcon.ElementalFury, 1156018, 1156019, duration, $"{_type}\t69\t{_maxAdd}"));
            }
        }

        FinishSequence();
    }

    public override void OnHit(Mobile defender, ref int damage)
    {
        _table ??= new Dictionary<Mobile, int>();

        _table.TryAdd(defender, 0);
        _table[defender] += Math.Min(_maxAdd, damage);

        if (defender is PlayerMobile pm)
        {
            pm.AddBuff(new BuffInfo(BuffIcon.ElementalFuryDebuff, 1155920, 1155920, default, ""));
        }

        if (_table[defender] < 69)
        {
            return;
        }

        defender.FixedParticles(0x3709, 10, 30, 5052, 2719, 0, EffectLayer.LeftFoot, 0);
        defender.PlaySound(0x208);

        var d = defender is PlayerMobile
            ? (int)(BaseSkillBonus / 6)
            : (int)(BaseSkillBonus / 3) + Utility.RandomMinMax(40, 60);

        switch (_type)
        {
            case ResistanceType.Physical:
                AOS.Damage(defender, Caster, d, 100, 0, 0, 0, 0, 0, DamageType.Spell);
                break;
            case ResistanceType.Fire:
                AOS.Damage(defender, Caster, d, 0, 100, 0, 0, 0, 0, DamageType.Spell);
                break;
            case ResistanceType.Cold:
                AOS.Damage(defender, Caster, d, 0, 0, 100, 0, 0, 0, DamageType.Spell);
                break;
            case ResistanceType.Poison:
                AOS.Damage(defender, Caster, d, 0, 0, 0, 100, 0, 0, DamageType.Spell);
                break;
            case ResistanceType.Energy:
                AOS.Damage(defender, Caster, d, 0, 0, 0, 0, 100, 0, DamageType.Spell);
                break;
        }

        if (defender is PlayerMobile defenderPm)
        {
            defenderPm.RemoveBuff(BuffIcon.ElementalFuryDebuff);
        }

        _table.Remove(defender);
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.ElementalFury);
        }

        if (_table == null)
        {
            return;
        }

        foreach (var m in _table.Keys)
        {
            if (m is PlayerMobile targetPm)
            {
                targetPm.RemoveBuff(BuffIcon.ElementalFuryDebuff);
            }
        }

        _table.Clear();
    }

    private static ResistanceType GetResistanceType(BaseWeapon weapon)
    {
        if (weapon == null)
        {
            return ResistanceType.Physical;
        }

        weapon.GetDamageTypes(null, out var phys, out var fire, out var cold, out var pois, out var nrgy, out _, out _);

        var highest = phys;
        var type = 0;

        if (fire > highest)
        {
            type = 1;
            highest = fire;
        }

        if (cold > highest)
        {
            type = 2;
            highest = cold;
        }

        if (pois > highest)
        {
            type = 3;
            highest = pois;
        }

        if (nrgy > highest)
        {
            type = 4;
        }

        return (ResistanceType)type;
    }
}
