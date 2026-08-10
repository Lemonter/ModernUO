using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class MahaonMystic : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public MahaonMystic() : base("мистик")
    {
        SetSkill(SkillName.Magery, 80.0, 100.0);
        Title = "Мистик";
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override VendorShoeType ShoeType => VendorShoeType.Sandals;

    public override void InitOutfit()
    {
        base.InitOutfit();
        AddItem(new Robe(Utility.RandomNondyedHue()));
    }

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBMahaonMystic());
    }
}

public class SBMahaonMystic : SBInfo
{
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
        public InternalBuyInfo()
        {
            Add(new GenericBuyInfo(typeof(SoulCatcherTattooNeedle), 5000, 20, 0x0F9F, 0));
            Add(new GenericBuyInfo(typeof(SakuroCraftingTool), 8000, 20, 0x0FB4, 0));
        }
    }

    public class InternalSellInfo : GenericSellInfo
    {
        public InternalSellInfo()
        {
            Add(typeof(SoulCatcherTattooNeedle), 1500);
            Add(typeof(SakuroCraftingTool), 2500);
        }
    }
}
