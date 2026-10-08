using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Onslaught.cs) —
// Swords mastery on-hit move: debuffs the defender's highest resistance for a few seconds.
public class OnslaughtMove : SkillMasteryMove
{
    public override int BaseMana => 20;
    public override double RequiredSkill => 90.0;

    public override SkillName MoveSkill => SkillName.Swords;
    public override TextDefinition AbilityMessage => 1156007; // *You ready an onslaught!*

    public override bool Validate(Mobile from)
    {
        if (!CheckWeapon(from))
        {
            from.SendLocalizedMessage(1156006); // You must have a swordsmanship weapon equipped to use this ability.
            return false;
        }

        return base.Validate(from);
    }

    public override void OnUse(Mobile from)
    {
        from.PlaySound(0x1EC);
        from.FixedEffect(0x3779, 10, 20, 1372, 0);
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
        if (!Validate(attacker) || !CheckMana(attacker, true))
        {
            return;
        }

        if (attacker.Weapon is not BaseWeapon weapon || HasOnslaught(attacker, defender))
        {
            return;
        }

        ClearCurrentMove(attacker);

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

        var resistType = (ResistanceType)type;

        var amount = (int)((attacker.Skills[MoveSkill].Value + attacker.Skills[SkillName.Tactics].Value) / 12);
        var duration = MasteryInfo.GetMasteryLevel(attacker, MoveSkill) * 2 + 1;

        if (defender is PlayerMobile)
        {
            amount /= 2;
        }

        var mod = new ResistanceMod(resistType, "MasteryOnslaught", -amount, defender);
        defender.AddResistanceMod(mod);

        attacker.PrivateOverheadMessage(MessageType.Regular, 1150, 1156008, attacker.NetState); // You deliver an onslaught of sword strikes!

        if (defender is PlayerMobile defenderPm)
        {
            // -~2_VAL~% ~1_RESIST~ Debuff.
            defenderPm.AddBuff(new BuffInfo(BuffIcon.Onslaught, 1156009, 1156010, TimeSpan.FromSeconds(duration), $"{amount}\t{resistType}"));
        }

        defender.FixedEffect(0x37B9, 10, 5, 632, 0);

        _table ??= new Dictionary<Mobile, Mobile>();
        _table[attacker] = defender;

        Timer.DelayCall(TimeSpan.FromSeconds(duration), () =>
        {
            defender.RemoveResistanceMod(mod);
            _table.Remove(attacker);
        });
    }

    public override void OnClearMove(Mobile from)
    {
        if (from is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Onslaught);
        }
    }

    private static Dictionary<Mobile, Mobile> _table;

    public static bool HasOnslaught(Mobile attacker, Mobile victim) =>
        _table != null && _table.TryGetValue(attacker, out var v) && v == victim;
}
