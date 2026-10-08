using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;

namespace Server.Systems.Bots;

/// <summary>
/// Bard skills in a fight, used the way a player does: pick the instrument, use the skill, aim the
/// cursor. Discordance weakens the foe; Peacemaking calms it while the bot gets away.
/// </summary>
public static class BotBard
{
    private const long SongDelayMs = 8_000;

    private static BaseInstrument PrepareInstrument(Mobile bot)
    {
        var instrument = bot.Backpack?.FindItemByType<BaseInstrument>();
        if (instrument != null)
        {
            BaseInstrument.SetInstrument(bot, instrument);
        }

        return instrument;
    }

    public static bool TryDiscord(BotBrain brain, Mobile foe) =>
        brain.Bot.Skills.Discordance.Value >= 50 && !Discordance.UnderEffects(foe) &&
        TryPlay(brain, SkillName.Discordance, foe);

    public static bool TryPeace(BotBrain brain, Mobile foe) =>
        brain.Bot.Skills.Peacemaking.Value >= 50 && foe is BaseCreature { BardPacified: false } &&
        TryPlay(brain, SkillName.Peacemaking, foe);

    private static bool TryPlay(BotBrain brain, SkillName skill, Mobile foe)
    {
        var bot = brain.Bot;
        var state = brain.Combat;
        var now = Core.TickCount;

        if (now - state.NextSongTick < 0 || bot.Target != null || bot.Spell != null || PrepareInstrument(bot) == null)
        {
            return false;
        }

        state.NextSongTick = now + SongDelayMs;

        if (!bot.UseSkill(skill) || bot.Target == null)
        {
            return false;
        }

        bot.Target.Invoke(bot, foe);
        return true;
    }
}
