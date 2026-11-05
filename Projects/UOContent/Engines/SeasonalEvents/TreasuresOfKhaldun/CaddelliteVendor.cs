using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/CaddelliteVendor.cs).
///     <c>SetWearable</c> becomes <c>AddItem</c>; the Caddellite tools are this codebase's plain marked subclasses
///     (ServUO's socket-based infusion does not exist here).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CaddelliteVendor : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public CaddelliteVendor() : base("жестянщик")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public static CaddelliteVendor InstanceTram { get; set; }

    public static CaddelliteVendor InstanceFel { get; set; }

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBCaddelliteTinker());
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");
        CantWalk = true;

        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
        HairItemID = Race.RandomHair(false);
        HairHue = Race.RandomHairHue();
    }

    public override void InitOutfit()
    {
        AddItem(new SmithHammer());
        AddItem(new LongPants { Hue = Utility.RandomBlueHue() });
        AddItem(new Shirt());
        AddItem(new FullApron());
        AddItem(new ThighBoots());
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Map == Map.Felucca)
        {
            InstanceFel = this;
        }

        if (Map == Map.Trammel)
        {
            InstanceTram = this;
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/CaddelliteVendor.cs).</summary>
public class SBCaddelliteTinker : SBInfo
{
    public SBCaddelliteTinker()
    {
        BuyInfo = new InternalBuyInfo();
    }

    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; }

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
        public InternalBuyInfo()
        {
            Add(new GenericBuyInfo(typeof(CaddellitePickaxe), 101267, 500, 3718, 0));
            Add(new GenericBuyInfo(typeof(CaddelliteHatchet), 101266, 500, 3907, 0));
            Add(new GenericBuyInfo(typeof(CaddelliteFishingPole), 101265, 500, 3520, 0));
        }
    }

    public class InternalSellInfo : GenericSellInfo
    {
    }
}
