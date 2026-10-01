using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Manual goal-setting for a [BecomeBot-possessed player — lets them force a specific
///     task (dig/chop/fight) instead of waiting for DoIdle's normal task roulette to land on
///     one eventually. Mirrors DoIdle's own TravelingToGather/TravelingToHunt setup exactly
///     (see BotSocial.cs) so the rest of the AI (BotGathering/BotHuntingDungeon) can't tell
///     the difference between a roulette pick and a player's explicit choice.
/// </summary>
public partial class BotController
{
    public static void SetGatherGoal(PlayerMobile bot, SkillName skill)
    {
        if (!Bots.TryGetValue(bot, out var profile))
        {
            return;
        }

        // Замок: с этого момента рулетка занятий отключена, пока человек не отменит.
        profile.ForcedGatherSkill = skill;
        profile.ForcedHunt = false;

        profile.GatherSkill = skill;
        profile.HarvestTarget = Utility.RandomMinMax(50, 70);
        profile.HarvestCount = 0;
        profile.GatherStartItemCount = -1;
        profile.GatherLastItemCount = -1;
        profile.GatherNoYieldStreak = 0;
        profile.GatherDestination = PickGatherSpot(bot, profile);
        profile.Activity = BotActivity.TravelingToGather;

        AnnounceNewActivity(bot, profile);
    }

    public static void SetHuntGoal(PlayerMobile bot)
    {
        if (!Bots.TryGetValue(bot, out var profile))
        {
            return;
        }

        profile.ForcedHunt = true;
        profile.ForcedGatherSkill = null;

        profile.HarvestTarget = Utility.RandomMinMax(50, 70);
        profile.HarvestCount = 0;
        profile.GatherStartItemCount = -1;
        profile.GatherLastItemCount = -1;
        profile.GatherNoYieldStreak = 0;
        profile.GatherDestination = bot is BotMobile huntBot && huntBot.PickKnownHuntingSpot() is { } known
            ? known
            : PickGatherSpot(bot, profile);
        profile.Activity = BotActivity.TravelingToHunt;

        AnnounceNewActivity(bot, profile);
    }

    /// <summary>Снимает замок — бот возвращается к собственному распорядку дня.</summary>
    public static void ClearForcedGoal(PlayerMobile bot)
    {
        if (Bots.TryGetValue(bot, out var profile))
        {
            profile.ForcedGatherSkill = null;
            profile.ForcedHunt = false;
        }
    }

    /// <summary>
    ///     Возобновляет приказанное занятие вместо рулетки. Возвращает false, если замка нет.
    ///
    ///     Место назначения выбирается ЗАНОВО: сюда мы попадаем как раз после того, как
    ///     прошлое не вышло — дерево оказалось недостижимым или участок выработан. Повторять
    ///     ту же точку значило бы зациклиться на ней.
    /// </summary>
    public static bool TryResumeForcedGoal(PlayerMobile bot, BotProfile profile)
    {
        if (profile.ForcedGatherSkill is { } skill)
        {
            SetGatherGoal(bot, skill);
            return true;
        }

        if (profile.ForcedHunt)
        {
            SetHuntGoal(bot);
            return true;
        }

        return false;
    }
}
