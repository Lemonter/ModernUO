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
        var casting = System.Math.Max(bot.Skills.Magery.Value, BotSchools.CasterSkill(bot));
        var weapon = bot.Weapon as BaseWeapon;
        var weaponSkill = weapon == null ? bot.Skills.Wrestling.Value : bot.Skills[weapon.Skill].Value;

        if (casting >= 50 && casting > weaponSkill)
        {
            return BotCombatStyle.Mage;
        }

        return weapon is BaseRanged ? BotCombatStyle.Archer : BotCombatStyle.Melee;
    }

    /// <summary>Arrows or bolts for this weapon in the quiver or the pack.</summary>
    public static bool HasAmmo(Mobile bot, System.Type ammo) =>
        bot.FindItemOnLayer<BaseQuiver>(Layer.Cloak)?.FindItemByType(ammo) != null || bot.Backpack?.FindItemByType(ammo) != null;

    /// <summary>
    /// Swaps an empty bow for the best melee weapon in the pack, or puts it away and fights with
    /// fists. Through the same moves a player makes: into the pack, then equip.
    /// </summary>
    public static void PutAwayRanged(Mobile bot)
    {
        if (bot.Weapon is not BaseRanged bow || bot.Backpack is not { } pack)
        {
            return;
        }

        BaseWeapon best = null;
        foreach (var weapon in pack.FindItemsByType<BaseWeapon>())
        {
            if (weapon is not BaseRanged && weapon.CanEquip(bot) &&
                (best == null || bot.Skills[weapon.Skill].Value > bot.Skills[best.Skill].Value))
            {
                best = weapon;
            }
        }

        pack.DropItem(bow);
        if (best != null)
        {
            bot.EquipItem(best);
        }
    }

    /// <summary>Whether this bot can fight at all with what it carries: a bow wants ammunition, a
    /// caster whose school needs reagents wants some in the pack. Supply buys them.</summary>
    public static bool IsArmed(Mobile bot)
    {
        if (bot.Weapon is BaseRanged { AmmoType: { } ammo } && !HasAmmo(bot, ammo))
        {
            return false;
        }

        if (Of(bot) != BotCombatStyle.Mage)
        {
            return true;
        }

        // Ткачество заклинаний реагентов не просит; магия, некромантия и мистицизм — просят.
        var reagentFree = bot.Skills.Spellweaving.Value >= System.Math.Max(bot.Skills.Magery.Value,
            System.Math.Max(bot.Skills.Necromancy.Value, bot.Skills.Mysticism.Value));

        return reagentFree || bot.Backpack?.FindItemByType<BaseReagent>() != null;
    }

    /// <summary>The skill that decides how hard a fight this bot can take.</summary>
    public static double FightingSkill(Mobile bot)
    {
        var weapon = bot.Weapon as BaseWeapon;
        var weaponSkill = weapon == null ? bot.Skills.Wrestling.Value : bot.Skills[weapon.Skill].Value;
        var casting = System.Math.Max(bot.Skills.Magery.Value, BotSchools.CasterSkill(bot));
        return System.Math.Max(casting, (weaponSkill + bot.Skills.Tactics.Value) / 2);
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
