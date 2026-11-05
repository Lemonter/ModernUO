using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Mobile/MiningCooperativeMerchant.cs).
///     It is not a real vendor (<c>IsActiveVendor</c> is false): double-clicking it opens its own purchase gump,
///     which sells ethereal sand commodity deeds against the <see cref="MiningCooperative" /> per-account cap.
///     <c>SetWearable</c> has no equivalent here, so the outfit is added with <see cref="Item.Hue" /> initialisers;
///     ServUO's three-argument <c>Banker.Withdraw(from, amount, true)</c> is the two-argument overload here (the
///     third argument only controlled account gold, which this codebase's bank handles itself).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MiningCooperativeMerchant : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public MiningCooperativeMerchant() : base("торговец шахтёрского кооператива")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public static MiningCooperativeMerchant InstanceTram { get; set; }

    public static MiningCooperativeMerchant InstanceFel { get; set; }

    public override bool IsActiveVendor => false;

    public override bool IsInvulnerable => true;

    public int MaxAmount => 5000;

    public int Price => 112;

    public int Quantity => 500;

    public override void InitSBInfo()
    {
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt { Hue = 0x3E4 });
        AddItem(new LongPants { Hue = 0x192 });
        AddItem(new Pickaxe());
        AddItem(new ThighBoots { Hue = 0x283 });
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(Location, 5))
        {
            from.CloseGump<MiningCooperativeGump>();
            from.SendGump(new MiningCooperativeGump(this, from));
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

    public class MiningCooperativeGump : Gump
    {
        private readonly MiningCooperativeMerchant _vendor;

        public MiningCooperativeGump(MiningCooperativeMerchant vendor, Mobile from) : base(100, 100)
        {
            _vendor = vendor;

            var available = vendor.MaxAmount - MiningCooperative.PurchaseAmount(from);

            AddPage(0);

            AddBackground(0, 0, 310, 350, 0x6DB);
            AddImage(54, 0, 0x6E4);
            AddHtmlLocalized(10, 10, 290, 18, 1114513, "#1154040", 0x0, false, false);
            AddItem(20, 80, 0xA3E8);
            AddHtmlLocalized(120, 73, 180, 18, 1159190, 0x43FF, false, false); // Ethereal Sand
            AddHtmlLocalized(120, 100, 180, 18, 1159191, $"{vendor.Price}", 0x43FF, false, false); // GP: ~1_VALUE~
            AddItem(20, 140, 0x14F0);
            AddHtmlLocalized(
                120, 143, 180, 18, 1159193, $"{vendor.Quantity}@{vendor.Quantity * vendor.Price}", 0x5FF0, false, false
            );
            AddHtmlLocalized(25, 203, 275, 18, 1159192, $"{available}@{vendor.MaxAmount}", 0x7FF0, false, false);
            AddHtmlLocalized(
                20, 243, 160, 72, 1159194, $"{vendor.Quantity}@#1159190@{vendor.Quantity * vendor.Price}", 0x7FFF, false, false
            );
            AddButton(220, 260, 0x81C, 0x81B, 1, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            var available = _vendor.MaxAmount - MiningCooperative.PurchaseAmount(from);
            var payment = _vendor.Quantity * _vendor.Price;

            if (info.ButtonID != 1)
            {
                return;
            }

            if (available <= 0)
            {
                // Begging thy pardon, but your family has purchased the maximum amount of that commodity.
                // I cannot sell you more until a new shipment arrives!
                _vendor.Say(1159195);
                return;
            }

            if (!Banker.Withdraw(from, payment))
            {
                _vendor.Say(500192); // Begging thy pardon, but thou canst not afford that.
                return;
            }

            var deed = new CommodityDeed();
            deed.SetCommodity(new EtherealSand(_vendor.Quantity));
            from.AddToBackpack(deed);

            MiningCooperative.AddPurchase(from, _vendor.Quantity);
            from.SendGump(new MiningCooperativeGump(_vendor, from));
        }
    }
}
