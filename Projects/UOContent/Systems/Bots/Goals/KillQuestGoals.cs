using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonQuests;

namespace Server.Systems.Bots;

public enum QuestGiverKind
{
    Mayor,
    Guard,
    Ranger,
    CourtMage
}

/// <summary>A bot's view of one kill quest: who gave it, what to kill, how far along.</summary>
public readonly record struct BotQuest(Mobile Giver, string Target, int Required, int Done)
{
    public bool IsComplete => Done >= Required;
}

/// <summary>The four "kill N of a kind" quest givers of the shard, as bots see them.</summary>
public static class BotQuests
{
    public static BotQuest? QuestOf(Mobile bot, QuestGiverKind kind) => kind switch
    {
        QuestGiverKind.Mayor when MayorQuestSystem.QuestOf(bot) is { } q =>
            new BotQuest(q.Mayor, q.TargetTypeName, q.RequiredKills, q.CurrentKills),
        QuestGiverKind.Guard when GuardQuestSystem.QuestOf(bot) is { } q =>
            new BotQuest(q.Sergeant, q.TargetTypeName, q.RequiredKills, q.CurrentKills),
        // Ranger kills count only once the heads are handed in, so heads carried count as done.
        QuestGiverKind.Ranger when RangerQuestSystem.QuestOf(bot) is { } q =>
            new BotQuest(q.Ranger, q.TargetTypeName, q.RequiredKills, q.CurrentKills + HeadsCarried(bot, q.TargetTypeName)),
        QuestGiverKind.CourtMage when CourtMageQuestSystem.QuestOf(bot) is { } q =>
            new BotQuest(q.Mage, q.TargetTypeName, q.RequiredKills, q.CurrentKills),
        _ => null
    };

    private static int HeadsCarried(Mobile bot, string animal)
    {
        var count = 0;
        if (bot.Backpack is { } pack)
        {
            foreach (var head in pack.FindItemsByType<MahaonAnimalHead>())
            {
                if (head.AnimalType == animal)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>A head the bot's ranger quest still needs.</summary>
    public static bool WantsTrophy(Mobile bot, Item item) =>
        item is MahaonAnimalHead head && QuestOf(bot, QuestGiverKind.Ranger) is { IsComplete: false } quest &&
        head.AnimalType == quest.Target;

    public static Mobile FindGiver(BotCity city, QuestGiverKind kind) => kind switch
    {
        QuestGiverKind.Mayor     => WorldCatalog.GetPlacedMobile<MahaonMayor>(city),
        QuestGiverKind.Guard     => WorldCatalog.GetPlacedMobile<MahaonGuardSergeant>(city),
        QuestGiverKind.Ranger    => WorldCatalog.GetPlacedMobile<MahaonRanger>(city),
        _                        => WorldCatalog.GetPlacedMobile<MahaonCourtMage>(city)
    };

    /// <summary>Talks to a giver as a player does: a double-click, or for the sergeant and the
    /// court mage the quest button of the gump their double-click opens.</summary>
    public static void Talk(Mobile bot, Mobile giver, QuestGiverKind kind)
    {
        switch (kind)
        {
            case QuestGiverKind.Guard:
                {
                    GuardQuestSystem.Talk(bot, giver);
                    break;
                }
            case QuestGiverKind.CourtMage:
                {
                    CourtMageQuestSystem.Talk(bot, giver);
                    break;
                }
            default:
                {
                    giver.OnDoubleClick(bot);
                    break;
                }
        }
    }
}

/// <summary>
/// Kill quests from a town's mayor, guard sergeant, forest ranger or court mage: a fighter takes
/// one when it has none, hunts the named kind where it spawns, and goes back to collect. Rewards
/// are the givers' own: gold, guard, ranger or court mage rank.
/// </summary>
public sealed class KillQuestGoal : BotGoal
{
    private const int MinFightingSkill = 40;

    // How far a bot walks to the hunting grounds of its quest.
    private const int SearchRange = 400;

    private readonly QuestGiverKind _kind;

    public KillQuestGoal(QuestGiverKind kind) => _kind = kind;

    public override string Name => _kind switch
    {
        QuestGiverKind.Mayor => "Задание мэра",
        QuestGiverKind.Guard => "Задание стражи",
        QuestGiverKind.Ranger => "Задание лесника",
        _ => "Задание придворного мага"
    };

    public override string[] News => ["Выполнил задание, получил награду.", "Задание сделано, можно и отдохнуть."];

    private static bool Fit(PlayerMobile bot) =>
        BotCombatStyles.FightingSkill(bot) >= MinFightingSkill && bot.Hits >= bot.HitsMax * 0.7;

    private HuntSpot SpotFor(PlayerMobile bot, BotQuest quest) =>
        HuntingAtlas.PickFor(bot.Map, bot.Location, SearchRange, quest.Target);

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        var quest = BotQuests.QuestOf(bot, _kind);

        if (quest is { IsComplete: true } done)
        {
            return done.Giver is { Deleted: false } && done.Giver.Map == bot.Map ? 0.75 : 0;
        }

        if (!Fit(bot))
        {
            return 0;
        }

        var diligence = BotBrain.Trait(brain.Diligence);

        if (quest is { } active)
        {
            return SpotFor(bot, active) != null ? 0.45 + diligence * 0.25 : 0;
        }

        // One quest at a time keeps a bot from collecting errands it never runs; a quest with no
        // hunting grounds in reach doesn't hold the others up.
        if (HasWorkableQuest(bot) || BotSocialRules.TownFor(bot) is not { } city || BotQuests.FindGiver(city, _kind) == null)
        {
            return 0;
        }

        var greed = _kind == QuestGiverKind.Mayor ? BotBrain.Trait(brain.Greed) * 0.1 : 0;
        return 0.25 + diligence * 0.2 + greed;
    }

    private static bool HasWorkableQuest(PlayerMobile bot)
    {
        foreach (var kind in Kinds)
        {
            if (BotQuests.QuestOf(bot, kind) is { } q && (q.IsComplete || HuntingAtlas.PickFor(bot.Map, bot.Location, SearchRange, q.Target) != null))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly QuestGiverKind[] Kinds =
        [QuestGiverKind.Mayor, QuestGiverKind.Guard, QuestGiverKind.Ranger, QuestGiverKind.CourtMage];

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var quest = BotQuests.QuestOf(bot, _kind);

        Mobile giver;
        if (quest is { IsComplete: true } done)
        {
            giver = done.Giver;
        }
        else if (quest is { } active)
        {
            var spot = SpotFor(bot, active);
            return spot == null
                ? null
                : [new GoToAction(spot.Map, spot.Center, 4, "на задание"), new HuntAction(spot, active.Required - active.Done, 0, active.Target)];
        }
        else
        {
            giver = BotSocialRules.TownFor(bot) is { } city ? BotQuests.FindGiver(city, _kind) : null;
        }

        return giver is not { Deleted: false }
            ? null
            : [new GoToAction(giver, 2, $"к {giver.Name}"), new TalkToGiverAction(giver, _kind)];
    }
}

public sealed class TalkToGiverAction : BotAction
{
    private readonly Mobile _giver;
    private readonly QuestGiverKind _kind;

    public TalkToGiverAction(Mobile giver, QuestGiverKind kind)
    {
        _giver = giver;
        _kind = kind;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_giver.Deleted || !bot.InRange(_giver, 3))
        {
            return BotActionResult.Failed();
        }

        bot.Direction = bot.GetDirectionTo(_giver);
        BotQuests.Talk(bot, _giver, _kind);
        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => $"Говорит с {_giver.Name}";
}
