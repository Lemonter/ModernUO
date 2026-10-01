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

    public static readonly IReadOnlyList<BotGoal> All = [Rest, Socialize, Bank, Travel, Loiter];
}
