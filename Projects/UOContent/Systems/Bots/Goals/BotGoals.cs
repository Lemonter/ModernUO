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
    public static readonly HuntGoal Hunt = new();
    public static readonly CorpseRunGoal CorpseRun = new();
    public static readonly TameGoal Tame = new();
    public static readonly TitheGoal Tithe = new();

    public static readonly IReadOnlyList<BotGoal> All = [Rest, Socialize, Bank, Travel, Loiter, Mine, Lumber, Fish, Trade, Supply, Learn, Smithing, Tinkering, Carpentry, Bowcraft, Hunt, CorpseRun, Tame, Tithe];
}
