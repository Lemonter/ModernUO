using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Items;
using Server.Gumps;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/KhaldunResearcher.cs).
///     ServUO's <c>BaseTurnInMobile</c> (turn in Cult-crested equipment for points) does not exist here, so the
///     researcher only opens the reward store; points are credited by <see cref="KhaldunData.AwardKill" />. His
///     display copies of the reward artifacts are packed as in ServUO; <c>SetWearable</c> becomes <c>AddItem</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunResearcher : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public KhaldunResearcher() : base("исследователь")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public static KhaldunResearcher InstanceTram { get; set; }

    public static KhaldunResearcher InstanceFel { get; set; }

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

        PackDisplayItem(new SeekerOfTheFallenStarTitleDeed());
        PackDisplayItem(new ZealotOfKhalAnkurTitleDeed());
        PackDisplayItem(new ProphetTitleDeed());
        PackDisplayItem(new CultistsRitualTome());
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

        list.Add(1158678); // Artifacts of the Cult Trader
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 5) && pm.CheckAlive())
        {
            pm.SendGump(new KhaldunRewardGump(this, pm));
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Map == Map.Trammel)
        {
            InstanceTram = this;
        }

        if (Map == Map.Felucca)
        {
            InstanceFel = this;
        }
    }
}
