using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.MLQuests;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     Quest givers of the "Myrmidex Threat" Eodon line, ported from ServUO
///     (Scripts/Quests/Eodon/MyrmidexThreat/Mobiles/Questers.cs). Quest lists are wired through
///     Distribution/Data/MLQuests.cfg (the canonical way in this codebase), so these are plain
///     <see cref="BaseCreature" />/<see cref="BaseVendor" /> NPCs rather than <c>MondainQuester</c> subclasses.
///
///     Dropped: the trap-training kits in the collector's buy list (<c>CircuitTrapTrainingKit</c>,
///     <c>CylinderTrapTrainingKit</c>, <c>SliderTrapTrainingKit</c>) do not exist in this codebase.
/// </summary>
[QuesterName("Яр (тинкер Барраб)")]
[SerializationGenerator(0, false)]
public partial class Yar : BaseCreature
{
    [Constructible]
    public Yar() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "тинкер Барраб";

        Body = 0x190;
        Hue = 34214;
        HairItemID = Race.RandomHair(false);

        InitStats(100, 100, 25);

        AddItem(new BoneChest { Hue = 1828 });
        AddItem(new DecorativePlateKabuto { Hue = 1828 });
        AddItem(new LeatherHaidate { Hue = 1828 });
        AddItem(new Sandals { Hue = 1828 });
        AddItem(new SledgeHammer { Hue = 1828 });
    }

    public override string DefaultName => "Яр";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override void GenerateLoot()
    {
    }
}

[QuesterName("Кэрролл (геммолог)")]
[SerializationGenerator(0, false)]
public partial class Carroll : BaseCreature
{
    [Constructible]
    public Carroll() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "геммолог";

        Body = 0x190;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair(false);

        InitStats(100, 100, 25);

        AddItem(new FancyShirt());
        AddItem(new JinBaori { Hue = 1366 });
        AddItem(new LongPants { Hue = 1336 });
        AddItem(new GoldNecklace());
        AddItem(new GoldBracelet());
        AddItem(new GoldRing());
        AddItem(new Shoes());
    }

    public override string DefaultName => "Кэрролл";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override void GenerateLoot()
    {
    }
}

[QuesterName("Бронт (капитан)")]
[SerializationGenerator(0, false)]
public partial class Bront : BaseCreature
{
    [Constructible]
    public Bront() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "капитан";

        Body = 0x190;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair(false);

        InitStats(100, 100, 25);

        AddItem(new TricorneHat());
        AddItem(new Epaulette());
        AddItem(new BodySash());
        AddItem(new ElvenShirt());
        AddItem(new WoodlandBelt());
        AddItem(new TattsukeHakama());
        AddItem(new Sandals());
    }

    public override string DefaultName => "Бронт";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override void GenerateLoot()
    {
    }
}

[QuesterName("Эриатвен (создательница големов)")]
[SerializationGenerator(0, false)]
public partial class Eriathwen : BaseCreature
{
    [Constructible]
    public Eriathwen() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "создательница големов";

        Race = Race.Elf;
        Female = true;
        Body = 605;
        Hue = 0x847E;
        HairItemID = Race.RandomHair(true);

        InitStats(100, 100, 25);

        AddItem(new ElvenShirt { Hue = 164 });
        AddItem(new ElvenPants { Hue = 1114 });
        AddItem(new ElvenBoots { Hue = 1828 });
    }

    public override string DefaultName => "Эриатвен";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override void GenerateLoot()
    {
    }
}

[QuesterName("Коллекционер диковин")]
[SerializationGenerator(0, false)]
public partial class CollectorOfOddities : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public CollectorOfOddities() : base("коллекционер диковин")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo() => _sbInfos.Add(new SBOddities());

    public override void InitOutfit()
    {
        AddItem(new FancyShirt { Hue = 1156 });
        AddItem(new Doublet { Hue = 1316 });
        AddItem(new ElvenPants { Hue = 1151 });
        AddItem(new Cloak { Hue = 1151 });
        AddItem(new ElvenBoots { Hue = 2007 });
    }

    private class SBOddities : SBInfo
    {
        private readonly List<GenericBuyInfo> _buyInfo = new()
        {
            new GenericBuyInfo("силовой блок стазис-камеры", typeof(StasisChamberPowerCore), 101250, 500, 40155, 0)
        };

        private readonly IShopSellInfo _sellInfo = new InternalSellInfo();

        public override IShopSellInfo SellInfo => _sellInfo;
        public override List<GenericBuyInfo> BuyInfo => _buyInfo;

        private class InternalSellInfo : GenericSellInfo
        {
        }
    }
}

[QuesterName("Элли Рафкин (профессор)")]
[SerializationGenerator(0, false)]
public partial class EllieRafkin : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public EllieRafkin() : base("профессор")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo() => _sbInfos.Add(new SBEodonMaps());

    public override void InitOutfit()
    {
        AddItem(new FancyShirt());
        AddItem(new Kilt { Hue = 933 });
        AddItem(new ThighBoots { Hue = 1 });
    }

    public override string DefaultName => "Элли Рафкин";

    private class SBEodonMaps : SBInfo
    {
        private readonly List<GenericBuyInfo> _buyInfo = new()
        {
            new GenericBuyInfo("несокращённый атлас Эодона", typeof(UnabridgedAtlasOfEodon), 62500, 500, 7185, 0)
        };

        private readonly IShopSellInfo _sellInfo = new InternalSellInfo();

        public override IShopSellInfo SellInfo => _sellInfo;
        public override List<GenericBuyInfo> BuyInfo => _buyInfo;

        private class InternalSellInfo : GenericSellInfo
        {
        }
    }
}

[QuesterName("Фокс (лейтенант)")]
[SerializationGenerator(0, false)]
public partial class Foxx : BaseCreature
{
    [Constructible]
    public Foxx() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "лейтенант";

        Body = 0x190;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair(true);

        InitStats(100, 100, 25);

        AddItem(new PlateChest());
        AddItem(new PlateLegs());
        AddItem(new BodySash { Hue = 1828 });
        AddItem(new OrderShield());
        AddItem(new Longsword());
    }

    public override string DefaultName => "Фокс";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override bool CanShout => true;

    public override void Shout(PlayerMobile pm)
    {
        // Fall in now! These Myrmidex aren't going to slay themselves!
        MLQuestSystem.Tell(this, pm, 1156619);
    }

    public override void GenerateLoot()
    {
    }
}

[QuesterName("Йеро (игрок)")]
[SerializationGenerator(0, false)]
public partial class Yero : BaseCreature
{
    [Constructible]
    public Yero() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "игрок";

        Body = 0x190;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair(true);

        InitStats(100, 100, 25);

        AddItem(new ShortPants());
        AddItem(new Sandals());
    }

    public override string DefaultName => "Йеро";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override void GenerateLoot()
    {
    }
}

[QuesterName("Алида (садовница)")]
[SerializationGenerator(0, false)]
public partial class Alida : BaseCreature
{
    [Constructible]
    public Alida() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "садовница";

        Female = true;
        Body = 0x191;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair(true);

        InitStats(100, 100, 25);

        AddItem(new Shirt());
        AddItem(new LongPants { Hue = 1 });
        AddItem(new HalfApron { Hue = 263 });
        AddItem(new LeatherGloves());
        AddItem(new FloppyHat());
    }

    public override string DefaultName => "Алида";

    public override bool IsInvulnerable => true;
    public override bool CanTeach => false;

    public override void GenerateLoot()
    {
    }
}
