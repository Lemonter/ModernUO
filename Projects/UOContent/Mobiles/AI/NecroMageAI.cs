using Server.Spells;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Necromancy;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/AI/Magical AI/NecromageAI.cs). Seven creatures
/// already in this codebase were built on it upstream and had to be dropped back to plain
/// AI_Mage for want of it — the liches, the Lifestealer, the Maddening Horror, Lady Melisande,
/// the Travesty and Niporailem.
///
/// This codebase's MageAI already picks necromancy damage and curse spells on its own, through
/// IsNecromancer/UseNecromancy, which ServUO's MageAI does not — so the halves of the original
/// that only exist to add those pools are already covered and are not repeated here. What is
/// genuinely its own, and is what this class carries:
///
/// - Spirit Speak as a self-heal, both in combat (a flat 10 % of heal attempts) and on guard,
///   where the chance scales by Necromancy the way the Magery heal scales by Magery.
/// - Curse Weapon and the Animate Dead summon in the self-buff slot.
///
/// MageAI's buff slot was a hardcoded Bless; it is now GetRandomBuffSpell, and HealChance went
/// from private to protected. No behaviour change to MageAI itself.</summary>
public class NecroMageAI : MageAI
{
    public NecroMageAI(BaseCreature m) : base(m)
    {
    }

    public override Spell GetRandomBuffSpell()
    {
        if (Utility.RandomBool())
        {
            return base.GetRandomBuffSpell();
        }

        if (!SmartAI && Utility.RandomBool())
        {
            return new CurseWeaponSpell(Mobile);
        }

        return GetRandomSummonSpell() ?? base.GetRandomBuffSpell();
    }

    /// <summary>A wild necromancer raises the dead around it; a summon or a pet does not.</summary>
    public virtual Spell GetRandomSummonSpell() =>
        !Mobile.Controlled && !Mobile.Summoned && Mobile.Mana >= 23 ? new AnimateDeadSpell(Mobile) : null;

    protected override Spell CheckCastHealingSpell()
    {
        if (Mobile.Summoned || Mobile.Hits >= Mobile.HitsMax)
        {
            return null;
        }

        if (Utility.RandomDouble() < 0.1)
        {
            Mobile.UseSkill(SkillName.SpiritSpeak);
            return null;
        }

        return base.CheckCastHealingSpell();
    }

    public override bool DoActionGuard()
    {
        if (Mobile.Controlled || Mobile.Summoned || Mobile.Combatant != null ||
            AcquireFocusMob(Mobile.RangePerception, Mobile.FightMode, false, false, true))
        {
            return base.DoActionGuard();
        }

        if (Mobile.Poisoned)
        {
            new CureSpell(Mobile).Cast();
            return true;
        }

        if (Mobile.Hits < Mobile.HitsMax - 30 &&
            ScaleBySkill(HealChance, SkillName.Necromancy) > Utility.RandomDouble())
        {
            Mobile.UseSkill(SkillName.SpiritSpeak);
            return true;
        }

        if (ScaleBySkill(HealChance, SkillName.Magery) > Utility.RandomDouble())
        {
            if (Mobile.Hits < Mobile.HitsMax - 50)
            {
                if (!new GreaterHealSpell(Mobile).Cast())
                {
                    new HealSpell(Mobile).Cast();
                }

                return true;
            }

            if (Mobile.Hits < Mobile.HitsMax - 10)
            {
                new HealSpell(Mobile).Cast();
                return true;
            }
        }

        return base.DoActionGuard();
    }

    /// <summary>Corpse Skin is pointless against something that only deals cold or physical
    /// damage — it lowers exactly those two resists.</summary>
    public static bool CheckCastCorpseSkin(BaseCreature bc) => bc.ColdDamage != 100 && bc.PhysicalDamage != 100;
}
