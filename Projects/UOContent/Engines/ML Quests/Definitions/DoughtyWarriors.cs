using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

/// <summary>
///     Doughty Warriors quest chain, given by Kane the Master of Arms. ServUO chains the three quests through
///     <c>QuestChain.DoughtyWarriors</c>; ModernUO has no quest-chain ids, so the chain is expressed with
///     <see cref="MLQuest.NextQuest" /> and <see cref="MLQuest.IsChainTriggered" /> instead.
/// </summary>
public class DoughtyWarriors : MLQuest
{
    public DoughtyWarriors()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1075379; // Doughty Warriors
        // Youngsters these days! Sorry, I didn't introduce myself. I'm Kane, the Master of Arms for this city. ...
        Description = 1075380;
        RefusalMessage = 1075382;    // Ah. I see. Never mind then.
        InProgressMessage = 1075383; // You'll find mongbats all over the place. ...
        CompletionMessage = 1075384; // Excellent! That's the old fighting spirit.
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(10, new[] { typeof(Mongbat) }, "монгбатов"));

        Rewards.Add(new DummyReward(1075381)); // Show them what a real warrior is made of!
    }

    public override Type NextQuest => typeof(DoughtyWarriorsPartTwo);
}

public class DoughtyWarriorsPartTwo : MLQuest
{
    public DoughtyWarriorsPartTwo()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1075404; // Doughty Warriors
        // You did great work. Thanks to you, my puppies are training harder than ever. ...
        Description = 1075405;
        RefusalMessage = 1075407;    // You're pretty sad. When I was a young warrior, I wasn't one to walk away from a challenge.
        InProgressMessage = 1075408; // You won't find many imps on this continent. ...
        CompletionMessage = 1075409; // Hah! I knew you were up to the challenge.
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(10, new[] { typeof(Imp) }, "импов"));

        Rewards.Add(new DummyReward(1075406)); // Show them what a real warrior is REALLY made of!
    }

    public override Type NextQuest => typeof(DoughtyWarriorsPartThree);
    public override bool IsChainTriggered => true;
}

public class DoughtyWarriorsPartThree : MLQuest
{
    public DoughtyWarriorsPartThree()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1075410; // Doughty Warriors
        // You've really inspired my trainees. They're working harder than ever. ...
        Description = 1075411;
        RefusalMessage = 1075413;    // What? You're not scared of a couple of lousy daemons, are you?
        InProgressMessage = 1075414; // There's all kinds of daemons you can kill. ...
        CompletionMessage = 1075415; // Thanks for helping me out. You're a real hero to my guards. ...
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(10, new[] { typeof(Daemon) }, "демонов"));

        Rewards.Add(
            new ItemReward(
                1075412, // Sash of Might
                typeof(SashOfMight)
            )
        );
    }

    public override bool IsChainTriggered => true;
}

[SerializationGenerator(0, false)]
public partial class Kane : BaseCreature
{
    [Constructible]
    public Kane() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "мастер оружия";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = Race.RandomSkinHue();

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x203C;
        HairHue = 0x3B3;

        AddItem(new PlateArms());
        AddItem(new PlateChest());
        AddItem(new PlateGloves());
        AddItem(new StuddedGorget());
        AddItem(new PlateLegs());

        switch (Utility.Random(4))
        {
            case 0:
                {
                    AddItem(new PlateHelm());
                    break;
                }
            case 1:
                {
                    AddItem(new NorseHelm());
                    break;
                }
            case 2:
                {
                    AddItem(new CloseHelm());
                    break;
                }
            case 3:
                {
                    AddItem(new Helmet());
                    break;
                }
        }

        switch (Utility.Random(3))
        {
            case 0:
                {
                    AddItem(new BodySash(0x482));
                    break;
                }
            case 1:
                {
                    AddItem(new Doublet(0x482));
                    break;
                }
            case 2:
                {
                    AddItem(new Tunic(0x482));
                    break;
                }
        }

        AddItem(new Broadsword());
        AddItem(
            new MetalKiteShield
            {
                Hue = Utility.RandomNondyedHue()
            }
        );
        AddItem(Utility.RandomBool() ? new Boots() : new ThighBoots());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Кейн";
    public override bool CanTeach => true;
    public override bool CanShout => true;

    public override void Shout(PlayerMobile pm)
    {
        // Come here, I have a task.  /  You there! I have a job for you.
        MLQuestSystem.Tell(this, pm, Utility.RandomBool() ? 1074186 : 1074183);
    }
}
