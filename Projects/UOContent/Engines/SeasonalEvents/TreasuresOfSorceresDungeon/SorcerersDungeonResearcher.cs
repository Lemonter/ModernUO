using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/SorcerersDungeonResearcher.cs).
///     ServUO's <c>BaseTurnInMobile</c> does not exist here, so the researcher only opens the reward store; points
///     are credited by <see cref="SorcerersDungeonData.AwardKill" />. <c>SetWearable</c> becomes <c>AddItem</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SorcerersDungeonResearcher : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public SorcerersDungeonResearcher() : base("исследователь")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public static SorcerersDungeonResearcher Instance { get; set; }

    public override bool IsActiveVendor => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");

        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
        HairItemID = 0x2047;
        HairHue = 0x46D;
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt { Hue = 1255 });
        AddItem(new JinBaori { Hue = 2722 });
        AddItem(new Kilt { Hue = 2012 });
        AddItem(new LongPants { Hue = 1775 });
        AddItem(new ThighBoots { Hue = 1908 });
        AddItem(new Necklace());

        PackDisplayItem(new HeroOfTheUnlovedTitleDeed());
        PackDisplayItem(new SaviorOfTheDementedTitleDeed());
        PackDisplayItem(new SlayerOfThePumpkinKingTitleDeed());
        PackDisplayItem(new SterlingSilverRing());
        PackDisplayItem(new TalonsOfEscaping());
        PackDisplayItem(new BootsOfEscaping());
    }

    private void PackDisplayItem(Item item)
    {
        item.Movable = false;
        PackItem(item);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1157617); // Artifacts of Enchanted Origin Trader
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 5) && pm.CheckAlive())
        {
            pm.SendGump(new SorcerersDungeonRewardGump(this, pm));
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Map == Map.Ilshenar)
        {
            Instance = this;
        }
    }
}
