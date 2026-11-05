using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

/// <summary>
///     The Honor of the De Boors quest chain, given by Derek the Merchant and turned in through Jack the Loan
///     Shark. ServUO chains the three quests through <c>QuestChain.HonorOfDeBoors</c> and overrides
///     <c>OnCompleted</c> to play the complete sound on the delivery step; ModernUO has no quest-chain ids and
///     plays the sound from the engine, so the chain is expressed with <see cref="MLQuest.NextQuest" /> and
///     <see cref="MLQuest.IsChainTriggered" /> and the sound override is dropped.
/// </summary>
public class HonorOfDeBoors : MLQuest
{
    public HonorOfDeBoors()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1075416; // The Honor of the De Boors
        // I beg your pardon, but will you listen to my story? My family, the de Boors family, have been jewel traders ...
        Description = 1075417;
        RefusalMessage = 1075419; // I know how much I am asking. Please, can you not help a dying man restore his family's honor?
        // Are you sure? You are very kind. Many of the monsters around here, when slain, are found to have jewels in their stomachs. ...
        InProgressMessage = 1075420;
        CompletionMessage = 1075421; // You have done it! Bless you! I do appreciate this very much! Though, will you do me one last favor?
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(10, typeof(Diamond), "алмазов"));
        Objectives.Add(new CollectObjective(10, typeof(Ruby), "рубинов"));
        Objectives.Add(new CollectObjective(10, typeof(Emerald), "изумрудов"));

        Rewards.Add(new DummyReward(1075418)); // Gather them quickly. Who knows how long Derek has to live?
    }

    public override Type NextQuest => typeof(JackTheVillain);
}

public class JackTheVillain : MLQuest
{
    public JackTheVillain()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1075422; // Jack the Villain
        Description = 1075423; // Will you take the jewels to the loan shark? I am not well enough to go myself, though it is not far.
        RefusalMessage = 1075425;    // Ah well. You have already helped me by gathering the jewels. I cannot complain.
        InProgressMessage = 1075426; // The name of the villain is Jack, you will find him over by the port.
        CompletionMessage = 1075427; // What do you want? Oh, that jewel merchant wants his shield back, eh?
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new DeliverObjective(
                typeof(BagOfJewels),
                1,
                1075307, // Bag of Jewels
                typeof(JackLoanShark)
            )
        );

        Rewards.Add(new DummyReward(1075424)); // Deliver the bag of jewels to the loan shark.
    }

    public override Type NextQuest => typeof(SavedHonor);
    public override bool IsChainTriggered => true;
}

public class SavedHonor : MLQuest
{
    public SavedHonor()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1075428; // Saved Honor
        // That idiot! This beat up piece of junk isn't worth more than three gold coins, four at most! ...
        Description = 1075429;
        RefusalMessage = 1075431;    // I don't care what you do!
        InProgressMessage = 1075432; // Go away and never come back.
        CompletionMessage = 1075433; // My shield! My family's honor! You have my gratitude. ...
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new DeliverObjective(
                typeof(DeBoorShield),
                1,
                1075308, // Ancestral Shield
                typeof(DerekMerchant)
            )
        );

        Rewards.Add(
            new ItemReward(
                1075430, // Goblet of Celebration
                typeof(GobletOfCelebration)
            )
        );
    }

    public override bool IsChainTriggered => true;
}

[SerializationGenerator(0, false)]
public partial class DerekMerchant : BaseCreature
{
    [Constructible]
    public DerekMerchant() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "торговец";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x8406;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x2048;
        HairHue = 0x473;
        FacialHairItemID = 0x204B;
        FacialHairHue = 0x473;

        AddItem(new Shoes());
        AddItem(new LongPants(0x901));
        AddItem(new FancyShirt(0x5F4));
        AddItem(new Backpack());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Дерек";
    public override bool CanTeach => true;
}

[SerializationGenerator(0, false)]
public partial class JackLoanShark : BaseCreature
{
    [Constructible]
    public JackLoanShark() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "ростовщик";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x83EC;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x2045;
        HairHue = 0x464;
        FacialHairItemID = 0x204B;
        FacialHairHue = 0x464;

        AddItem(new Dagger());
        AddItem(new ThighBoots(0x901));
        AddItem(new LongPants(0x521));
        AddItem(new FancyShirt(0x5A7));
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Джек";
    public override bool CanTeach => true;
}
