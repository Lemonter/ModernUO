using Server.Engines.MLQuests.Items;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

/// <summary>
///     Ratmen Sanctuary quests, given by Danoel (Sanctuary, Ilshenar). ServUO restricts the two kill objectives to
///     the "Sanctuary" region; ModernUO's <see cref="KillObjective" /> has no region parameter, so kills count
///     anywhere. ServUO's TrinketBag/TreasureBag reward items are replaced with the equivalent ModernUO reward bags
///     (<see cref="BagOfTrinkets" /> / <see cref="BagOfTreasure" />), keeping ServUO's reward clilocs.
/// </summary>
public class SpringCleaning : MLQuest
{
    public SpringCleaning()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1072373; // Spring Cleaning
        // Mangy, lice encrusted furballs!  Those filthy ratmen have been sneaking into camp again ...
        Description = 1072674;
        RefusalMessage = 1072684;    // I understand.  The stench in those tunnels is unbearable.
        InProgressMessage = 1072685; // The ratmen have holes all over the place that lead to their warrens. ...
        CompletionMessage = 1075384; // Excellent! That's the old fighting spirit.
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(15, new[] { typeof(Ratman) }, "ратманов"));

        Rewards.Add(new ItemReward(1072341, typeof(BagOfTrinkets))); // trinket bag
    }
}

public class Cowards : MLQuest
{
    public Cowards()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1072689; // Cowards!
        // When you were exterminating those untidy vermin, you must have noticed the archers amongst them? ...
        Description = 1072697;
        RefusalMessage = 1072684;    // I understand.  The stench in those tunnels is unbearable.
        InProgressMessage = 1072685; // The ratmen have holes all over the place that lead to their warrens. ...
        CompletionMessage = 1075409; // Hah! I knew you were up to the challenge.
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(10, new[] { typeof(RatmanArcher) }, "ратманов-лучников"));

        Rewards.Add(new ItemReward(1072341, typeof(BagOfTrinkets))); // trinket bag
    }
}

public class TokenOfLove : MLQuest
{
    public TokenOfLove()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1072690; // Token of Love
        // Argh!  I knew those ratmen were up to no good!  They have stolen the ring I was making for my beloved Ciala. ...
        Description = 1072698;
        RefusalMessage = 1072699;    // Please, I beg of you, reconsider!
        InProgressMessage = 1072700; // The stolen ring is easy enough to recognize. ...
        CompletionMessage = 1074582; // Ah!  You found my property.  Thank you for your honesty in returning it to me.
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(StolenRing), 1073124)); // A ring engraved: "Beloved Ciala"

        Rewards.Add(new ItemReward(1072583, typeof(BagOfTreasure))); // treasure bag
    }
}
