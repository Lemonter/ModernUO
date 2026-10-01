using Server.Items;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Fourth;

namespace Server.Systems.Bots;

/// <summary>
/// Keeping a bot alive with what a player would use: bandages (healing skill), heal and cure
/// potions, and for casters Heal, Greater Heal and Cure. Everything goes through the item or
/// spell itself, so delays, reagents, skill checks and interruptions apply.
/// </summary>
public static class BotHealing
{
    // Spacing between potions, about the heal potion's own drink delay.
    private const long PotionDelayMs = 10_000;

    /// <summary>Tries one healing measure. True when something was started this tick.</summary>
    public static bool TryHeal(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;
        if (pack == null || !bot.Alive)
        {
            return false;
        }

        var hp = (double)bot.Hits / bot.HitsMax;

        // Potions first in an emergency: instant, no skill.
        var now = Core.TickCount;
        var potionReady = now - brain.Combat.NextPotionTick >= 0;

        if (potionReady && hp < 0.35 && !bot.Poisoned && pack.FindItemByType<BaseHealPotion>() is { } healPotion)
        {
            brain.Combat.NextPotionTick = now + PotionDelayMs;
            healPotion.OnDoubleClick(bot);
            return true;
        }

        if (potionReady && bot.Poisoned && pack.FindItemByType<BaseCurePotion>() is { } curePotion)
        {
            brain.Combat.NextPotionTick = now + PotionDelayMs;
            curePotion.OnDoubleClick(bot);
            return true;
        }

        // A bandage heals and cures, and doesn't stop the fighting.
        if ((hp < 0.75 || bot.Poisoned) && bot.Skills.Healing.Value >= 20 && BandageContext.GetContext(bot) == null &&
            pack.FindItemByType<Bandage>() is { } bandage && BandageContext.BeginHeal(bot, bot) != null)
        {
            bandage.Consume();
            return true;
        }

        if (bot.Spell != null || bot.Target != null)
        {
            return false;
        }

        if (bot.Poisoned && (bot.Skills.Magery.Value >= 30 && TryCast(brain, new CureSpell(bot), bot) ||
                             BotSchools.TryCast(brain, SchoolUse.Cure, bot)))
        {
            return true;
        }

        if (hp < 0.6 && BotSchools.TryCast(brain, SchoolUse.Heal, bot))
        {
            return true;
        }

        if (hp < 0.6 && bot.Skills.Magery.Value >= 50 && TryCast(brain, new GreaterHealSpell(bot), bot))
        {
            return true;
        }

        return hp < 0.6 && bot.Skills.Magery.Value >= 20 && TryCast(brain, new HealSpell(bot), bot);
    }

    /// <summary>Starts a spell aimed at <paramref name="target"/>; the target is handed over when
    /// the spell asks for it (see <see cref="BotCombat"/>).</summary>
    public static bool TryCast(BotBrain brain, Spell spell, Mobile target)
    {
        var bot = brain.Bot;
        if (bot.Mana < spell.GetMana() || !spell.Cast())
        {
            return false;
        }

        brain.Combat.SpellTarget = target;
        return true;
    }

    /// <summary>Heals the most wounded group member within reach: bandage, Greater Heal or Close
    /// Wounds, whichever the bot can do. True when something was started.</summary>
    public static bool TryHealAlly(BotBrain brain, BotGroup group)
    {
        var bot = brain.Bot;
        Mobile patient = null;
        var worst = 0.6;

        foreach (var m in group.Members)
        {
            if (m == bot || !m.Alive || m.Map != bot.Map || !bot.InRange(m, 10))
            {
                continue;
            }

            var hp = (double)m.Hits / m.HitsMax;
            if (hp < worst || m.Poisoned && worst > 0.59)
            {
                worst = hp;
                patient = m;
            }
        }

        if (patient == null)
        {
            return false;
        }

        if (bot.Skills.Healing.Value >= 30 && bot.InRange(patient, 2) && BandageContext.GetContext(bot) == null &&
            bot.Backpack?.FindItemByType<Bandage>() is { } bandage && BandageContext.BeginHeal(bot, patient) != null)
        {
            bandage.Consume();
            return true;
        }

        if (bot.Spell != null || bot.Target != null)
        {
            return false;
        }

        if (patient.Poisoned && bot.Skills.Magery.Value >= 30 && TryCast(brain, new CureSpell(bot), patient))
        {
            return true;
        }

        if (bot.Skills.Magery.Value >= 50 && TryCast(brain, new GreaterHealSpell(bot), patient))
        {
            return true;
        }

        return bot.Skills.Chivalry.Value >= 30 && TryCast(brain, new Spells.Chivalry.CloseWoundsSpell(bot), patient);
    }
}
