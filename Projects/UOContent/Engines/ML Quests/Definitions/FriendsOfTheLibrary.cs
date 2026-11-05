using System;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

/// <summary>
///     Friends of the Library quest chain, given by Verity (Britain library), Sarakki the notary and Petrus the
///     bee keeper. ServUO gates the chain on <c>MondainsLegacy.PublicDonations</c> and flags the player with
///     <c>PlayerMobile.LibraryFriend</c> on completion; neither exists in ModernUO, so the gate and the flag are
///     dropped (the chain is always available and grants the membership token item instead).
/// </summary>
public class FriendsOfTheLibrary : MLQuest
{
    public FriendsOfTheLibrary()
    {
        Activated = true;
        Title = 1072716; // Friends of the Library
        // Shhh!  *angry whisper*  This is a library, not some bawdy house!  ...
        Description = 1072720;
        RefusalMessage = 1072721;    // Hrmph!
        InProgressMessage = 1072722; // *glare*  Shhhh!  You need to visit the notary.  She can be found near the castle.
        CompletionMessage = 1073985; // Greetings!
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new DeliverObjective(
                typeof(FriendsOfTheLibraryApplication),
                1,
                1073131, // Friends of the Library Application
                typeof(Sarakki)
            )
        );

        Rewards.Add(new DummyReward(1072749)); // Friends of the Library Membership Token.
    }

    public override Type NextQuest => typeof(BureaucraticDelay);
}

public class BureaucraticDelay : MLQuest
{
    public BureaucraticDelay()
    {
        Activated = true;
        Title = 1072717; // Bureaucratic Delay
        // What do you have there?  Oh.  *look of dismay*  It seems everyone is interested in helping the library ...
        Description = 1072724;
        RefusalMessage = 1072725;    // I do apologize for being unprepared. Perhaps when you return later I'll have more wax in stock.
        InProgressMessage = 1072726; // Petrus lives in Ilshenar, past the Compassion gate and beyond the gypsy camp.
        CompletionMessage = 1073986; // Hello, hello.
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new DeliverObjective(
                typeof(SealingWaxOrderAddressedToPetrus),
                1,
                1073132, // Sealing Wax Order addressed to Petrus
                typeof(Petrus)
            )
        );

        Rewards.Add(new DummyReward(1074871)); // A step closer to having sealing wax.
    }

    public override Type NextQuest => typeof(TheSecretIngredient);
    public override bool IsChainTriggered => true;
}

public class TheSecretIngredient : MLQuest
{
    public TheSecretIngredient()
    {
        Activated = true;
        Title = 1072718; // The Secret Ingredient
        // What's this?  Sealing wax ... Sarakki ... official documents ... Oh, I see.  Can do, can do. ...
        Description = 1072727;
        RefusalMessage = 1072728;    // I see. I see.  Well good luck to you.
        InProgressMessage = 1072729; // Hello, hello!  Speckled, that's what they are. ...
        CompletionMessage = 1073987; // Fine, fine.  Do you have them?
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new CollectObjective(
                5,
                typeof(SpeckledPoisonSac),
                1073133 // Speckled Poison Sac
            )
        );

        Rewards.Add(new DummyReward(1074871)); // A step closer to having sealing wax.
    }

    public override Type NextQuest => typeof(SpecialDelivery);
    public override bool IsChainTriggered => true;
}

public class SpecialDelivery : MLQuest
{
    public SpecialDelivery()
    {
        Activated = true;
        Title = 1072719; // Special Delivery
        // Good good!  The wax is just cooling now and will be ready by the time you get it back to Sarakki. ...
        Description = 1072741;
        RefusalMessage = 1072746;    // Sorry, sorry.  I'll hold onto your order in case you change your mind.
        InProgressMessage = 1072747; // Hello, hello.  No rush of course. ...
        CompletionMessage = 1073988; // Oh welcome back!  Do you have my wax?
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new DeliverObjective(
                typeof(OfficialSealingWax),
                1,
                1072744, // Official Sealing Wax
                typeof(Sarakki)
            )
        );

        Rewards.Add(new DummyReward(1072749)); // Friends of the Library Membership Token.
    }

    public override Type NextQuest => typeof(AccessToTheStacks);
    public override bool IsChainTriggered => true;
}

public class AccessToTheStacks : MLQuest
{
    public AccessToTheStacks()
    {
        Activated = true;
        Title = 1072723; // Access to the Stacks
        // There you are!  *pleased smile*  Don't you just love when a form is all filled in like that? ...
        Description = 1072748;
        RefusalMessage = 1072750;    // Oh dear!  You've changed your mind?  *looks flustered* ...
        InProgressMessage = 1072751; // The librarian can always be found in the Library. ...
        CompletionMessage = 1074811; // As an official friend of the library you can make contributions at a donation area.
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new DeliverObjective(
                typeof(NotarizedApplication),
                1,
                1073135, // Notarized Application
                typeof(Verity)
            )
        );

        Rewards.Add(
            new ItemReward(
                1073136, // Friend of the Library Token (allows donations to be made)
                typeof(FriendOfTheLibraryToken)
            )
        );
    }

    public override bool IsChainTriggered => true;
}
