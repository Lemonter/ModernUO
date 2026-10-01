using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions;

/// <summary>The Royal City quest givers of Ter Mur, ported from ServUO
/// (Scripts/Mobiles/NPCs/ and Scripts/Quests/). Eight of them and nine quests: the three
/// artificers of the Soulforge, the bladeweaver who hunts the Void, the seer and the noble who
/// sends you to him, the librarian driven out of Bedlam, and the fence.
///
/// As with the Underworld line these are expressed in this codebase's own MLQuest form rather
/// than porting ServUO's second BaseQuest/MondainQuester framework alongside it — clilocs,
/// objective counts and rewards are the original's.
///
/// Four of the original's Royal City NPCs are deliberately absent, each blocked on a system
/// this shard does not have: Axem (the museum collection and Loyalty Rating), Thepem (alchemy
/// bulk orders), Zosilem and Percolem (tiered quests).</summary>
public class KnowledgeoftheSoulforge : MLQuest
{
    public KnowledgeoftheSoulforge()
    {
        Activated = true;
        Title = "Knowledge of the Soulforge";
        Description = 1112526;
        RefusalMessage = 1112546;
        InProgressMessage = 1112547;
        CompletionMessage = 1112527;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(50, typeof(EnchantedEssence), "Enchanted Essence"));

        Rewards.Add(new ItemReward("Knowledge", typeof(ScrollBox)));
    }
}

public class MasteringtheSoulforge : MLQuest
{
    public MasteringtheSoulforge()
    {
        Activated = true;
        Title = "Mastering the Soulforge";
        Description = 1112529;
        RefusalMessage = 1112549;
        InProgressMessage = 1112550;
        CompletionMessage = 1112551;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(50, typeof(RelicFragment), "Relic Fragments"));

        Rewards.Add(new ItemReward("Knowledge", typeof(ScrollBox2)));
    }
}

public class SecretsoftheSoulforge : MLQuest
{
    public SecretsoftheSoulforge()
    {
        Activated = true;
        Title = "Secrets of the Soulforge";
        Description = 1112522;
        RefusalMessage = 1112523;
        InProgressMessage = 1112547;
        CompletionMessage = 1112524;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(50, typeof(MagicalResidue), "Magical Residue"));

        Rewards.Add(new ItemReward("Knowledge", typeof(ScrollBox3)));
    }
}

/// <summary>Ansikart's smaller commission — one gem for one imbuing ingredient.</summary>
public class ALittleSomething : MLQuest
{
    public ALittleSomething()
    {
        Activated = true;
        Title = "A Little Something";
        Description = 1113773;
        RefusalMessage = 1113774;
        InProgressMessage = 1113775;
        CompletionMessage = 1113776;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(1, typeof(BrilliantAmber), "Brilliant Amber"));

        Rewards.Add(new ItemReward(1112994, typeof(MeagerImbuingBag)));
    }
}

/// <summary>Agralem's standing grievance. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Agralem.cs). Any ten void creatures count — the objective matches on
/// BaseVoidCreature, exactly as the original does, so every stage of every evolution line
/// qualifies.</summary>
public class IntoTheVoid : MLQuest
{
    public IntoTheVoid()
    {
        Activated = true;
        Title = 1112687;       // Into the Void
        Description = 1112690;
        RefusalMessage = 1112691;
        InProgressMessage = 1112692;
        CompletionMessage = 1112693;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new KillObjective(10, new[] { typeof(BaseVoidCreature) }, "Void Daemons"));

        Rewards.Add(new ItemReward(1112694, typeof(AbyssReaver)));
    }
}

/// <summary>Agralem the Bladeweaver. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Agralem.cs).</summary>
[SerializationGenerator(0, false)]
public partial class Agralem : BaseCreature
{
    [Constructible]
    public Agralem() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Bladeweaver";
        Race = Race.Gargoyle;
        Hue = 34536;

        HairItemID = 0x425D;
        HairHue = 0x31D;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        SetSkill(SkillName.Anatomy, 65.0, 90.0);
        SetSkill(SkillName.MagicResist, 65.0, 90.0);
        SetSkill(SkillName.Tactics, 65.0, 90.0);
        SetSkill(SkillName.Throwing, 65.0, 90.0);

        AddItem(new Cyclone());
        AddItem(new GargishLeatherKiltType1 { Hue = 2305 });
        AddItem(new GargishLeatherChestType1 { Hue = 2305 });
        AddItem(new GargishLeatherArmsType1 { Hue = 2305 });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Агралем";

    public override bool CanShout => true;
    public override void Shout(PlayerMobile pm) => Say(1112688); // Daemons from the void! They must be vanquished!
}

/// <summary>Egwexem cannot leave his post, so you carry his writ to the seer. Ported from
/// ServUO (Scripts/Mobiles/NPCs/Egwexem.cs).
///
/// Its in-progress line is a plain English string upstream rather than a cliloc — translated
/// here like the rest of this shard's free text. The original also gives it a twelve-hour
/// restart delay and a done-once flag; this codebase's MLQuest has neither knob, so the quest
/// is simply repeatable.</summary>
public class RumorsAbound : MLQuest
{
    public RumorsAbound()
    {
        Activated = true;
        Title = 1112514;       // Rumors Abound
        Description = 1112515;
        RefusalMessage = 1112516;
        InProgressMessage = "Ты ещё не говорил с Наксатиллором. Ступай к нему!";
        CompletionMessage = 1112518;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new DeliverObjective(typeof(EgwexemWrit), 1, "Egwexem's Writ", typeof(Naxatilor)));

        Rewards.Add(new DummyReward(1112731));
    }
}

/// <summary>Naxatillor sends you against whichever of the three undead gargoyles the roll picks
/// — the original rolls once, when the quest is created. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Naxatillor.cs).</summary>
public class TheArisen : MLQuest
{
    public TheArisen()
    {
        Activated = true;
        Title = 1112538;       // The Arisen
        Description = 1112539;
        RefusalMessage = 1112540;
        InProgressMessage = 1112517;
        CompletionMessage = 1112543;
        CompletionNotice = CompletionNoticeShort;

        var (type, name) = Utility.Random(3) switch
        {
            0 => (typeof(GargoyleShade), "Gargoyle Shade"),
            1 => (typeof(EffetePutridGargoyle), "Effete Putrid Gargoyle"),
            _ => (typeof(EffeteUndeadGargoyle), "Effete Undead Gargoyle")
        };

        Objectives.Add(new KillObjective(10, new[] { type }, name));

        Rewards.Add(new ItemReward(1113137, typeof(NecklaceofDiligence)));
    }
}

/// <summary>Master Cohenn lost his thesis to the dead of Bedlam. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Cohenn.cs). His reward is the key to the Bedlam library podium, which
/// this codebase already has from the peerless work.</summary>
public class Misplaced : MLQuest
{
    public Misplaced()
    {
        Activated = true;
        Title = 1074438;       // Misplaced
        Description = 1074439;
        RefusalMessage = 1074441;
        InProgressMessage = 1074442;
        CompletionMessage = 1074443;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(5, typeof(DisintegratingThesisNotes), "disintegrating thesis notes"));

        Rewards.Add(new ItemReward(1074347, typeof(LibrariansKey)));
    }
}

/// <summary>The fence's trade. Ported from ServUO (Scripts/Quests/UnusualGoods.cs).
///
/// Its second reward upstream is a Loyalty Rating point; this shard has no loyalty system, so
/// only the box is handed over.</summary>
public class UnusualGoods : MLQuest
{
    public UnusualGoods()
    {
        Activated = true;
        Title = 1113787;       // Unusual Goods
        Description = 1113788;
        RefusalMessage = 1113789;
        InProgressMessage = 1113790;
        CompletionMessage = 1113791;
        CompletionNotice = CompletionNoticeShort;

        Objectives.Add(new CollectObjective(2, typeof(PerfectEmerald), "Perfect Emerald"));
        Objectives.Add(new CollectObjective(1, typeof(CrystallineBlackrock), "Crystalline Blackrock"));

        Rewards.Add(new ItemReward(1113770, typeof(EssenceBox)));
    }
}

/// <summary>Aurvidlem the Artificer. Ported from ServUO (Scripts/Mobiles/NPCs/Aurvidlem.cs).
/// The three artificers are the same NPC in three hues, each holding one rung of the Soulforge
/// ladder.</summary>
[SerializationGenerator(0, false)]
public partial class Aurvidlem : BaseCreature
{
    [Constructible]
    public Aurvidlem() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Artificer";
        Race = Race.Gargoyle;
        Hue = 0x86DE;

        HairItemID = 0x4259;
        HairHue = 0x0;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        SetSkill(SkillName.ItemID, 60.0, 83.0);
        SetSkill(SkillName.Imbuing, 60.0, 83.0);

        AddItem(new SerpentstoneStaff());
        AddItem(new GargishClothChestType1 { Hue = 1307 });
        AddItem(new GargishClothArmsType1 { Hue = 1330 });
        AddItem(new GargishClothKiltType1 { Hue = 1307 });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Аурвидлем";

    public override bool CanShout => true;
    public override void Shout(PlayerMobile pm) => Say(1112525); // Come to be Artificer. I have a task for you.
}

/// <summary>Ansikart the Artificer. Ported from ServUO (Scripts/Mobiles/NPCs/Ansikart.cs).</summary>
[SerializationGenerator(0, false)]
public partial class Ansikart : BaseCreature
{
    [Constructible]
    public Ansikart() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Artificer";
        Race = Race.Gargoyle;
        Hue = 0x86DF;

        HairItemID = 0x425D;
        HairHue = 0x321;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        SetSkill(SkillName.ItemID, 60.0, 83.0);
        SetSkill(SkillName.Imbuing, 60.0, 83.0);

        AddItem(new SerpentstoneStaff());
        AddItem(new GargishClothChestType1 { Hue = 1428 });
        AddItem(new GargishClothArmsType1 { Hue = 1445 });
        AddItem(new GargishClothKiltType1 { Hue = 1443 });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Ансикарт";

    public override bool CanShout => true;
    public override void Shout(PlayerMobile pm) => Say(1112528); // Master the art of unraveling magic.
}

/// <summary>Beninort the Artificer. Ported from ServUO (Scripts/Mobiles/NPCs/Beninort.cs).</summary>
[SerializationGenerator(0, false)]
public partial class Beninort : BaseCreature
{
    [Constructible]
    public Beninort() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Artificer";
        Race = Race.Gargoyle;
        Hue = 0x86E8;

        HairItemID = 0x4258;
        HairHue = 0x31D;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        SetSkill(SkillName.ItemID, 60.0, 83.0);
        SetSkill(SkillName.Imbuing, 60.0, 83.0);

        AddItem(new SerpentstoneStaff());
        AddItem(new GargishClothChestType1 { Hue = 1609 });
        AddItem(new GargishClothArmsType1 { Hue = 1651 });
        AddItem(new GargishClothKiltType1 { Hue = 1649 });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Бенинорт";

    public override bool CanShout => true;
    public override void Shout(PlayerMobile pm) => Say(1112521); // Know the secrets. Learn of the soulforge.
}

/// <summary>Egwexem the Noble. Ported from ServUO (Scripts/Mobiles/NPCs/Egwexem.cs).</summary>
[SerializationGenerator(0, false)]
public partial class Egwexem : BaseCreature
{
    [Constructible]
    public Egwexem() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Noble";
        Body = 666;
        Female = false;

        HairItemID = 16987;
        HairHue = 1801;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        AddItem(new Backpack());
        AddItem(new GargishClothChestType1());
        AddItem(new GargishClothKiltType1());
        AddItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Эгвексем";
}

/// <summary>Naxatillor the Seer. Ported from ServUO (Scripts/Mobiles/NPCs/Naxatillor.cs) —
/// where the file is spelled with two Ls and the class with one, a split the original itself
/// papers over with a TypeAlias and a spawner rewrite. The class name here is the one the
/// original settled on.</summary>
[SerializationGenerator(0, false)]
[TypeAlias("Server.Mobiles.Naxatillor")]
public partial class Naxatilor : BaseCreature
{
    [Constructible]
    public Naxatilor() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "The Seer";
        Body = 666;
        Female = false;

        HairItemID = 16987;
        HairHue = 1801;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        AddItem(new Backpack());
        AddItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Наксатиллор";
}

/// <summary>Master Cohenn, the librarian driven out of Bedlam. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Cohenn.cs).</summary>
[SerializationGenerator(0, false)]
public partial class Cohenn : BaseCreature
{
    [Constructible]
    public Cohenn() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the librarian";
        Race = Race.Human;
        Female = false;
        Hue = 0x840C;

        HairItemID = 0x2045;
        HairHue = 0x453;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        AddItem(new Backpack());
        AddItem(new Sandals(0x74A));
        AddItem(new Robe(0x498));
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "мастер Кохенн";
}

/// <summary>Sliem the Fence. Ported from ServUO (Scripts/Mobiles/NPCs/Sliem.cs).</summary>
[SerializationGenerator(0, false)]
public partial class Sliem : BaseCreature
{
    [Constructible]
    public Sliem() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "the Fence";
        Body = 666;
        Female = false;

        HairItemID = 16987;
        HairHue = 1801;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        AddItem(new Backpack());
        AddItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Слием";
}
