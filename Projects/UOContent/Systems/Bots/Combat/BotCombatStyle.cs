using Server.Items;

namespace Server.Systems.Bots;

public enum BotCombatStyle : byte
{
    Melee,
    Archer,
    Mage
}

/// <summary>How a bot fights, read from what it actually knows and carries — never assigned: a
/// warrior who trains Magery to 80 starts casting, an archer whose bow broke closes in.</summary>
public static class BotCombatStyles
{
    public static BotCombatStyle Of(Mobile bot)
    {
        var magery = bot.Skills.Magery.Value;
        var weapon = bot.Weapon as BaseWeapon;
        var weaponSkill = weapon == null ? bot.Skills.Wrestling.Value : bot.Skills[weapon.Skill].Value;

        if (magery >= 50 && magery > weaponSkill)
        {
            return BotCombatStyle.Mage;
        }

        return weapon is BaseRanged ? BotCombatStyle.Archer : BotCombatStyle.Melee;
    }

    /// <summary>The skill that decides how hard a fight this bot can take.</summary>
    public static double FightingSkill(Mobile bot)
    {
        var weapon = bot.Weapon as BaseWeapon;
        var weaponSkill = weapon == null ? bot.Skills.Wrestling.Value : bot.Skills[weapon.Skill].Value;
        var magery = bot.Skills.Magery.Value;
        return System.Math.Max(magery, (weaponSkill + bot.Skills.Tactics.Value) / 2);
    }

    /// <summary>
    /// The toughest creature (by fame, which in UO tracks difficulty closely) this bot will pick a
    /// fight with: quadratic in skill, so a novice hunts rabbits and a grandmaster dragons.
    /// Aggressive bots reach higher, cautious ones lower.
    /// </summary>
    public static int MaxPreyFame(BotBrain brain)
    {
        var skill = FightingSkill(brain.Bot);
        var nerve = 0.7 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.6;
        return (int)((300 + skill * skill * 1.2) * nerve);
    }
}
