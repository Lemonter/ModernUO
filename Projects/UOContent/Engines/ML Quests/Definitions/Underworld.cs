using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

/// <summary>The Underworld quest line, ported from ServUO (Scripts/Quests/Vernix.cs and its
/// siblings). The dungeon, its regions and Navrey were already here; the quests and the
/// goblins who give them were not, which is why `Vernix`, `Jaacar`, `Barreraak`, `Xenrr`,
/// `Dugan`, `Gretchen`, `Tobin` and `QuartermasterFlint` all appear in badspawn.log — the
/// spawners in Distribution/XmlSpawner/underworld.xml are named after them and have been
/// failing to resolve.
///
/// ServUO writes these against its own BaseQuest/MondainQuester framework. This codebase has a
/// complete quest system of its own (265 quests in Data/MLQuests.cfg) with the same
/// cliloc-and-objectives shape, so the quests are expressed in MLQuest form rather than
/// porting a second, redundant framework. Titles, descriptions, refusals, progress and
/// completion clilocs, objective counts and rewards are the original's.</summary>
public class UntanglingTheWeb : MLQuest
{
    public UntanglingTheWeb()
    {
        Activated = true;
        Title = 1095050;       // Untangling the Web
        Description = 1095052; // Kill Acid Slugs and Acid Elementals to fill Vernix's jars.
        RefusalMessage = 1095053;
        InProgressMessage = 1095054;
        CompletionMessage = 1095057;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new KillObjective(
                12,
                new[] { typeof(AcidSlug), typeof(AcidElemental) },
                "acid slugs and acid elementals"
            )
        );

        // ServUO hands over an AcidPopper directly; the item is already in this codebase.
        Rewards.Add(new ItemReward(1095058, typeof(AcidPopper)));
    }

    public override Type NextQuest => typeof(GreenWithEnvy);
}

/// <summary>The second half of Vernix's line: proof that Navrey is dead.
///
/// One substitution, flagged rather than hidden: the original's reward is a RewardBox, a
/// generic ServUO quest container with no equivalent class here. ItemReward.BagOfTreasure is
/// what this codebase's own quests use in that role.</summary>
public class GreenWithEnvy : MLQuest
{
    public GreenWithEnvy()
    {
        Activated = true;
        Title = 1095118;       // Green with Envy
        Description = 1095120; // Slay Navrey Night-Eyes and bring back proof of the deed.
        RefusalMessage = 1095121;
        InProgressMessage = 1095122;
        CompletionMessage = 1095123;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(EyeOfNavrey), "Eye of Navrey"));

        Rewards.Add(ItemReward.BagOfTreasure);
    }

    public override bool IsChainTriggered => true;
}

/// <summary>Elder Dugan's second commission: thin out the gray goblins that have been harrying
/// the settler camp. Ported from ServUO (Scripts/Quests/EndingtheThreat.cs).</summary>
public class EndingTheThreat : MLQuest
{
    public EndingTheThreat()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1095012;       // Ending the Threat
        Description = 1095014; // Travel to the Stygian Abyss and slay the gray goblins.
        RefusalMessage = 1095015;
        InProgressMessage = 1095016;
        CompletionMessage = 1095017;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new KillObjective(
                10,
                new[] { typeof(GrayGoblin) },
                "gray goblins",
                new QuestArea(1095190, "Abyss") // the Abyss
            )
        );

        Rewards.Add(ItemReward.LargeBagOfTreasure);
    }
}

/// <summary>The prospector who leads the human settlers in the Underworld. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Dugan.cs).
///
/// She offers two quests in the original: "Missing" first, then "Ending the Threat" once that
/// and the escort quest are done. Only the second is here — see the file header for the two
/// items "Missing" needs that this codebase doesn't have yet.</summary>
[SerializationGenerator(0, false)]
public partial class Dugan : BaseCreature
{
    [Constructible]
    public Dugan() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Prospector";
        Race = Race.Human;
        Body = 0x190;
        Female = true;
        Hue = 0x83EA;

        HairItemID = 0x203C;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;
        Direction = Direction.West;

        AddItem(new Backpack());
        AddItem(new Shoes(1819));
        AddItem(new LeatherArms());
        AddItem(new LeatherChest());
        AddItem(new LeatherLegs());
        AddItem(new LeatherGloves());
        AddItem(new GnarledStaff());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Дуган";
}

/// <summary>Elder Dugan's first commission: find out what became of the adventurers who went
/// into the mountain. Ported from ServUO (Scripts/Quests/Missing.cs).</summary>
public class Missing : MLQuest
{
    public Missing()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1094949;       // Missing
        Description = 1094951; // Recover evidence from the fallen adventurers below.
        RefusalMessage = 1094952;
        InProgressMessage = 1094953;
        CompletionMessage = 1094956;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(
            new CollectObjective(4, typeof(ArielHavenWritofMembership), "Ariel Haven Writs of Membership")
        );

        Rewards.Add(new ItemReward(1094957, typeof(CandlewoodTorch)));
    }
}

/// <summary>Neville's escort down to the settler camp. Ported from ServUO
/// (Scripts/Quests/EscortToDugan.cs).</summary>
public class EscortToDugan : MLQuest
{
    public EscortToDugan()
    {
        Activated = true;
        OneTimeOnly = true;
        Title = 1095003;       // The Lost Brightwhistle
        Description = 1095005; // See Neville Brightwhistle safely to Elder Dugan.
        RefusalMessage = 1095006;
        InProgressMessage = 1095007;
        CompletionMessage = 1095008;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new EscortObjective(new QuestArea(1113623, "NPC Encampment")));

        Rewards.Add(new ItemReward(1095011, typeof(TalismanofGoblinSlaying)));
    }
}

/// <summary>Jaacar's commission: make the green goblins afraid of you. Ported from ServUO
/// (Scripts/Quests/BadCompany.cs). It chains on to "A Tangled Web".</summary>
public class BadCompany : MLQuest
{
    public BadCompany()
    {
        Activated = true;
        Title = 1095022;       // Bad Company
        Description = 1095024; // Slay green goblins until they learn to fear you.
        RefusalMessage = 1095025;
        InProgressMessage = 1095026;
        CompletionMessage = 1095030;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(10, new[] { typeof(GreenGoblin) }, "green goblins"));

        Rewards.Add(new ItemReward(1113608, typeof(JaacarBox)));
    }

    public override Type NextQuest => typeof(ATangledWeb);
}

/// <summary>The rest of Jaacar's errand: a barrel of blood, drawn from whatever in the
/// Underworld has any. Ported from ServUO (Scripts/Quests/ATangledWeb.cs).
///
/// The objective matches on IBloodCreature rather than on a list of types, exactly as the
/// original does — this codebase's KillObjective tests AcceptedTypes with IsAssignableFrom, so
/// an interface works directly and any creature later marked with it counts without touching
/// the quest. Bloodworms and blood elementals carry it today.
///
/// The original's reward is a LargeTreasureBag with cliloc 1072706; this codebase's own
/// LargeBagOfTreasure carries the same cliloc and the same role, so it is that reward, not a
/// substitute.</summary>
public class ATangledWeb : MLQuest
{
    public ATangledWeb()
    {
        Activated = true;
        Title = 1095032;       // A Tangled Web
        Description = 1095034; // Kill Bloodworms and Blood Elementals to fill Jaacar's barrel.
        RefusalMessage = 1095035;
        InProgressMessage = 1095036;
        CompletionMessage = 1095038;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(12, new[] { typeof(IBloodCreature) }, "blood creatures"));

        Rewards.Add(ItemReward.LargeBagOfTreasure);
    }

    public override bool IsChainTriggered => true;
}

/// <summary>Tobin wants specimens of the goblins' floor traps. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Tobin.cs).</summary>
public class DoneInTheNameOfTinkering : MLQuest
{
    public DoneInTheNameOfTinkering()
    {
        Activated = true;
        Title = 1094983;       // Done in the Name of Tinkering
        Description = 1094985; // Find five floor traps in the Abyss and bring the components back.
        RefusalMessage = 1094986;
        InProgressMessage = 1094987;
        CompletionMessage = 1094988;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(5, typeof(FloorTrapComponent), "Floor Trap Components"));

        Rewards.Add(new ItemReward(1113293, typeof(GoblinFloorTrapKit)));
    }
}

/// <summary>Xenrr's fishing trip. Ported from ServUO (Scripts/Mobiles/NPCs/Xenrr.cs).
///
/// The title cliloc is 1095059, which is also Barreraak's — that's a copy-paste in the
/// original, comment and all, kept rather than second-guessed.</summary>
public class ScrapingTheBottom : MLQuest
{
    public ScrapingTheBottom()
    {
        Activated = true;
        Title = 1095059;
        Description = 1095061; // Catch a mud puppy.
        RefusalMessage = 1095062;
        InProgressMessage = 1095063;
        CompletionMessage = 1095065;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(MudPuppy), "Mud Puppy"));

        Rewards.Add(new ItemReward(1095066, typeof(XenrrFishingPole)));
    }
}

/// <summary>Barreraak's fishing trip. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Barreraak.cs).</summary>
public class SomethingFishy : MLQuest
{
    public SomethingFishy()
    {
        Activated = true;
        Title = 1095059;
        Description = 1095043; // Catch a red herring.
        RefusalMessage = 1095044;
        InProgressMessage = 1095045;
        CompletionMessage = 1095048;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(RedHerring), "Red Herring"));

        Rewards.Add(new ItemReward(1095049, typeof(BarreraaksRing)));
    }
}

/// <summary>The survivor who needs walking down to the settler camp. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Neville.cs), where the class is `Neville` and the display name is
/// "Neville Brightwhistle".
///
/// This codebase's own spawn data (Data/Spawns/**) asks for `NevilleBrightwhistle`, so the
/// class takes that name to match the data — the same mismatch as Navrey, resolved the other
/// way round because nothing here is named `Neville` yet.
///
/// ServUO respawns him automatically through a static Instances list; that isn't ported —
/// he's placed by the spawner, as everything else in this dungeon is.</summary>
[SerializationGenerator(0, false)]
public partial class NevilleBrightwhistle : BaseEscortable
{
    private static readonly string[] _cries =
    {
        "Save Us",
        "Murder is being done!",
        "Protect me!",
        "a scoundrel is committing murder!",
        "Where are the guards! Help!",
        "Make haste",
        "Tisawful! Death! Ah!"
    };

    private long _nextCry;

    [Constructible]
    public NevilleBrightwhistle()
    {
        Race = Race.Human;
        Female = false;
        Hue = Race.RandomSkinHue();

        SpeechHue = 0x3B2;

        InitStats(100, 100, 25);

        Utility.AssignRandomHair(this);

        AddItem(new Backpack());
        AddItem(new Shoes(0x70A));
        AddItem(new LongPants(0x1BB));
        AddItem(new FancyShirt(0x588));
    }

    public override string DefaultName => "Невилл Брайтвистл";

    public override bool CanBeDamaged() => false;

    public override void OnThink()
    {
        base.OnThink();

        if (_nextCry > Core.TickCount || !Alive || ControlMaster == null)
        {
            return;
        }

        if (!ControlMaster.Hidden && ControlMaster.Aggressors.Count > 0)
        {
            Say(_cries.RandomElement());
            _nextCry = Core.TickCount + Utility.RandomMinMax(20000, 30000);
        }
    }
}

/// <summary>Quartermaster Flint's first job: the goblins have made off with his barley.
/// Ported from ServUO (Scripts/Mobiles/NPCs/QuartermasterFlint.cs).</summary>
public class ThievesBeAfoot : MLQuest
{
    public ThievesBeAfoot()
    {
        Activated = true;
        Title = 1094958;       // Thieves Be Afoot!
        Description = 1094960; // Recover four barrels of barley from the Underworld.
        RefusalMessage = 1094961;
        InProgressMessage = 1094962;
        CompletionMessage = 1094965;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(4, typeof(BarrelOfBarley), "barrels of barley"));

        Rewards.Add(new ItemReward(1094967, typeof(BottleOfFlintsPungnentBrew)));
    }

    public override Type NextQuest => typeof(Bibliophile);
}

/// <summary>And his second: the logbook went with the barley.</summary>
public class Bibliophile : MLQuest
{
    public Bibliophile()
    {
        Activated = true;
        Title = 1094968;       // Bibliophile
        Description = 1094970; // Recover Flint's stolen logbook.
        RefusalMessage = 1094971;
        InProgressMessage = 1094972;
        CompletionMessage = 1094975;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(FlintsLogbook), "Flint's logbook"));

        Rewards.Add(new ItemReward(1113608, typeof(KegOfFlintsPungnentBrew)));
    }

    public override bool IsChainTriggered => true;
}

/// <summary>The settlers' quartermaster. Ported from ServUO
/// (Scripts/Mobiles/NPCs/QuartermasterFlint.cs).</summary>
[SerializationGenerator(0, false)]
public partial class QuartermasterFlint : BaseCreature
{
    [Constructible]
    public QuartermasterFlint() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Race = Race.Human;
        Body = 0x190;
        Female = false;
        Hue = 0x8418;

        HairItemID = 0x2046;
        HairHue = 0x466;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        AddItem(new Backpack());
        AddItem(new Shoes(0x743));
        AddItem(new LongPants());
        AddItem(new FancyShirt());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "квартирмейстер Флинт";
}

/// <summary>Gretchen's shopping list. Ported from ServUO (Scripts/Mobiles/NPCs/Gretchen.cs,
/// where the quest is declared alongside the NPC).
///
/// The original writes its own refusal and in-progress lines as English strings rather than
/// clilocs; they are translated here like the rest of this shard's free text. Title and
/// objective names are hers too — 1094978/1094981 are the only clilocs she has.</summary>
public class Curiosities : MLQuest
{
    public Curiosities()
    {
        Activated = true;
        Title = 1094978;       // Curiosities
        Description = 1094978;
        RefusalMessage = "Такая работа тебя пугает? Ха!";
        InProgressMessage = "Жаль, что ты ещё не взялся за дело.";
        CompletionMessage = 1094981;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(3, typeof(FertileDirt), "Fertile Dirt"));
        Objectives.Add(new CollectObjective(3, typeof(Bone), "Bone"));

        Rewards.Add(new ItemReward(1095147, typeof(ExplodingTarPotion)));
    }
}

/// <summary>The alchemist of the settler camp. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Gretchen.cs). Her quest is "Curiosities", above.</summary>
[SerializationGenerator(0, false)]
public partial class Gretchen : BaseCreature
{
    [Constructible]
    public Gretchen() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Alchemist";
        Race = Race.Elf;
        Female = true;
        Hue = 33767;

        HairItemID = 0x2047;
        HairHue = 0x465;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;
        Direction = Direction.East;

        AddItem(new Backpack());
        AddItem(new Shoes(1886));
        AddItem(new FemaleElvenRobe(443));
        AddItem(new QuarterStaff());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Гретхен";
}

/// <summary>Ported from ServUO (Scripts/Mobiles/NPCs/Jaacar.cs). His quest, "Bad Company",
/// waits on the JaacarBox reward class.</summary>
[SerializationGenerator(0, false)]
public partial class Jaacar : BaseCreature
{
    [Constructible]
    public Jaacar() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Body = 723;
        Female = false;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Frozen = true;
        Direction = Direction.Down;

        AddItem(new Backpack());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Джаакар";
}

/// <summary>Ported from ServUO (Scripts/Mobiles/NPCs/Tobin.cs). His quest, "Done in the Name of
/// Tinkering", waits on the GoblinFloorTrapKit reward class; the trap components it asks for
/// are already in Engines/Peerless/Underworld/UnderworldQuestItems.cs.</summary>
[SerializationGenerator(0, false)]
public partial class Tobin : BaseCreature
{
    [Constructible]
    public Tobin() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Tinkerer";
        Race = Race.Human;
        Body = 0x190;
        Female = false;
        Hue = 0x8418;

        HairItemID = 0x2046;
        HairHue = 0x466;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        AddItem(new Backpack());
        AddItem(new Shoes(0x743));
        AddItem(new Shirt(0x743));
        AddItem(new ShortPants(0x485));
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Тобин-непоседа";
}

/// <summary>Ported from ServUO (Scripts/Mobiles/NPCs/Xenrr.cs). His quest, "Scraping the
/// Bottom", waits on the XenrrFishingPole reward class; the mud puppy it asks for is already
/// ported.</summary>
[SerializationGenerator(0, false)]
public partial class Xenrr : BaseCreature
{
    [Constructible]
    public Xenrr() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Body = 723;
        Female = false;

        HairItemID = 0x2044;
        HairHue = 1153;
        FacialHairItemID = 0x204B;
        FacialHairHue = 1153;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Blessed = true;

        AddItem(new Backpack());
        AddItem(new Boots());
        AddItem(new LongPants(0x6C7));
        AddItem(new FancyShirt(0x6BB));
        AddItem(new Cloak(0x59));
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Зенрр";
}

/// <summary>Ported from ServUO (Scripts/Mobiles/NPCs/Barreraak.cs). His quest, "Something
/// Fishy", waits on the BarreraaksRing reward class; the red herring it asks for is already
/// ported.</summary>
[SerializationGenerator(0, false)]
public partial class Barreraak : BaseCreature
{
    [Constructible]
    public Barreraak() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Body = 334;
        Female = false;

        HairItemID = 0x2044;
        HairHue = 1153;
        FacialHairItemID = 0x204B;
        FacialHairHue = 1153;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Blessed = true;

        AddItem(new Backpack());
        AddItem(new Boots());
        AddItem(new LongPants(0x6C7));
        AddItem(new FancyShirt(0x6BB));
        AddItem(new Cloak(0x59));
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Баррераак";
}

/// <summary>The goblin who sends you after the Underworld's acid creatures, and then after
/// Navrey. Ported from ServUO (Scripts/Quests/Vernix.cs).
///
/// He is frozen facing east where the spawner puts him, as in the original.</summary>
[SerializationGenerator(0, false)]
public partial class Vernix : BaseCreature
{
    [Constructible]
    public Vernix() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Body = 723;
        Female = false;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Frozen = true;
        Direction = Direction.East;

        AddItem(new Backpack());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Верникс";
}
