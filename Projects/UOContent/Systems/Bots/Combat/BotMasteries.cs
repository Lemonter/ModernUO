using System;
using System.Collections.Generic;
using Server.Items;
using Server.Spells;
using Server.Spells.SkillMasteries;
using Server.Systems.MahaonMasteries;

namespace Server.Systems.Bots;

/// <summary>
/// The SA skill masteries as a bot uses them. A bot follows the mastery of its best skill it has
/// learned (from a tome) and can use (90 and up): it sets it active, as the book of masteries'
/// choice does, which also switches on that path's passive; and in a fight it now and then calls
/// on one of the path's abilities — a spell cast through the spell itself, or a mastery move armed
/// for the next swing. Mana, cooldowns and requirements are the ability's own business.
/// </summary>
public static class BotMasteries
{
    // How often, per fighting think, a bot reaches for a mastery ability.
    private const double UseChance = 0.3;

    // A mastery move keeps its cooldown on its instance, so each kind is armed from one instance.
    private static readonly Dictionary<Type, SkillMasteryMove> _moves = new();

    /// <summary>The best mastery skill the bot can use: 90 and up, whether learned or not.</summary>
    public static SkillName? BestSkill(Mobile bot)
    {
        SkillName? best = null;
        var bestValue = (double)MasteryInfo.MinSkillRequirement;
        foreach (var skill in MasteryInfo.Skills)
        {
            var value = bot.Skills[skill].Value;
            if (value >= bestValue)
            {
                bestValue = value;
                best = skill;
            }
        }

        return best;
    }

    /// <summary>The mastery path the bot follows: its best usable skill it has learned.</summary>
    public static SkillName? Path(Mobile bot)
    {
        SkillName? best = null;
        var bestValue = (double)MasteryInfo.MinSkillRequirement;
        foreach (var skill in MasteryInfo.Skills)
        {
            var value = bot.Skills[skill].Value;
            if (value >= bestValue && MasteryInfo.HasLearned(bot, skill))
            {
                bestValue = value;
                best = skill;
            }
        }

        return best;
    }

    /// <summary>The volume of the next tome the bot wants for its best skill, or 0.</summary>
    public static int WantedPrimerVolume(Mobile bot) =>
        BestSkill(bot) is { } skill && MasteryInfo.GetMasteryLevel(bot, skill) < 3 ? MasteryInfo.GetMasteryLevel(bot, skill) + 1 : 0;

    /// <summary>A tome the bot would read: for a skill it can use the mastery of, a volume it lacks.</summary>
    public static bool Reads(Mobile bot, SkillMasteryPrimer primer) =>
        bot.Skills[primer.Skill].Value >= MasteryInfo.MinSkillRequirement && !MasteryInfo.HasLearned(bot, primer.Skill, primer.Volume);

    /// <summary>Makes the bot's path its active mastery, switching on the path's passive.</summary>
    public static void EnsureActive(Mobile bot)
    {
        if (Path(bot) is not { } skill)
        {
            return;
        }

        var current = MasteryState.GetCurrentMastery(bot);
        if (current != skill)
        {
            MasteryState.SetCurrentMastery(bot, skill);
            MasteryInfo.OnMasteryChanged(bot, current);
        }
    }

    /// <summary>Calls on one of the path's abilities in a fight; true when one started.</summary>
    public static bool TryUse(BotBrain brain, Mobile foe)
    {
        var bot = brain.Bot;
        if (bot.Spell != null || bot.Target != null || Utility.RandomDouble() >= UseChance || Path(bot) is not { } path)
        {
            return false;
        }

        EnsureActive(bot);

        var abilities = new List<Type>();
        foreach (var info in MasteryInfo.Infos)
        {
            if (info.MasterySkill == path && info.SpellType != null)
            {
                abilities.Add(info.SpellType);
            }
        }

        // Start somewhere random so a path with several abilities uses them all.
        var start = abilities.Count > 0 ? Utility.Random(abilities.Count) : 0;
        for (var i = 0; i < abilities.Count; i++)
        {
            var type = abilities[(start + i) % abilities.Count];

            if (typeof(SkillMasteryMove).IsAssignableFrom(type))
            {
                if (SpecialMove.GetCurrentMove(bot) == null && WeaponAbility.GetCurrentAbility(bot) == null &&
                    SpecialMove.SetCurrentMove(bot, MoveOf(type)))
                {
                    return true;
                }

                continue;
            }

            // An upkeep ability already running would be switched off by a second cast.
            if (SkillMasterySpell.HasSpell(bot, type) || SkillMasterySpell.IsInCooldown(bot, type, false))
            {
                continue;
            }

            if (Activator.CreateInstance(type, bot, null) is Spell spell && BotHealing.TryCast(brain, spell, foe))
            {
                return true;
            }
        }

        return false;
    }

    private static SkillMasteryMove MoveOf(Type type)
    {
        if (!_moves.TryGetValue(type, out var move))
        {
            _moves[type] = move = (SkillMasteryMove)Activator.CreateInstance(type);
        }

        return move;
    }
}
