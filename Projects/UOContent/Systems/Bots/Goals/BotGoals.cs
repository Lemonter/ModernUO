using System.Collections.Generic;

namespace Server.Systems.Bots;

/// <summary>The goals every bot weighs on each review. Stage by stage this grows into the full
/// set — work, hunting, dungeons, PvP; the brain needs no change for that.</summary>
public static class BotGoals
{
    public static readonly RestGoal Rest = new();
    public static readonly SocializeGoal Socialize = new();
    public static readonly BankGoal Bank = new();
    public static readonly TravelGoal Travel = new();
    public static readonly LoiterGoal Loiter = new();
    public static readonly GatherGoal Mine = new(ResourceKind.Ore);
    public static readonly GatherGoal Lumber = new(ResourceKind.Wood);
    public static readonly GatherGoal Fish = new(ResourceKind.Fish);
    public static readonly TradeGoal Trade = new();
    public static readonly SupplyGoal Supply = new();
    public static readonly LearnGoal Learn = new();
    public static readonly CraftGoal Smithing = new(0);
    public static readonly CraftGoal Tinkering = new(1);
    public static readonly CraftGoal Carpentry = new(2);
    public static readonly CraftGoal Bowcraft = new(3);
    public static readonly CraftGoal Tailoring = new(4);
    public static readonly CraftGoal Alchemy = new(5);
    public static readonly CraftGoal Cooking = new(6);
    public static readonly CraftGoal Inscription = new(7);
    public static readonly HuntGoal Hunt = new();
    public static readonly CorpseRunGoal CorpseRun = new();
    public static readonly TameGoal Tame = new();
    public static readonly TitheGoal Tithe = new();
    public static readonly LeadGroupGoal LeadGroup = new();
    public static readonly FollowGroupGoal FollowGroup = new();
    public static readonly PkGoal Pk = new();
    public static readonly OutfitGoal Outfit = new();
    public static readonly StableGoal Stable = new();
    public static readonly FarmGoal Farm = new();
    public static readonly WeaveGoal Weave = new();
    public static readonly SowGoal Sow = new();
    public static readonly BuildHouseGoal BuildHouse = new();
    public static readonly FurnishGoal Furnish = new();
    public static readonly BuyCityHomeGoal BuyCityHome = new();
    public static readonly FurnishCityHomeGoal FurnishCityHome = new();
    public static readonly DefendTownGoal DefendTown = new();
    public static readonly ShelterGoal Shelter = new();

    public static readonly GuildOrderGoal GuildOrder = new();
    public static readonly BountyHuntGoal BountyHunt = new();

    public static readonly KillQuestGoal MayorQuest = new(QuestGiverKind.Mayor);
    public static readonly KillQuestGoal GuardQuest = new(QuestGiverKind.Guard);
    public static readonly KillQuestGoal RangerQuest = new(QuestGiverKind.Ranger);
    public static readonly KillQuestGoal CourtMageQuest = new(QuestGiverKind.CourtMage);

    public static readonly ClaimCityGoal ClaimCity = new();
    public static readonly SiegeCityGoal SiegeCity = new();
    public static readonly DefendCityGoal DefendCity = new();

    public static readonly IReadOnlyList<BotGoal> All = [Rest, Socialize, Bank, Travel, Loiter, Mine, Lumber, Fish, Trade, Supply, Learn, Smithing, Tinkering, Carpentry, Bowcraft, Tailoring, Alchemy, Cooking, Inscription, Hunt, CorpseRun, Tame, Tithe, LeadGroup, FollowGroup, Pk, Outfit, Stable, Farm, Weave, Sow, BuildHouse, Furnish, BuyCityHome, FurnishCityHome, DefendTown, Shelter, GuildOrder, BountyHunt, MayorQuest, GuardQuest, RangerQuest, CourtMageQuest, ClaimCity, SiegeCity, DefendCity];
}
