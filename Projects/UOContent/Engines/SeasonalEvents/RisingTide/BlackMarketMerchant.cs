using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/BlackMarketMerchant.cs).
///     ServUO's <c>BaseTurnInMobile</c> (sell maritime cargo for doubloons) does not exist here, so the merchant
///     carries that part itself: cargo handed over (dropped on the merchant, or via the context menu) is bought
///     for doubloons through <see cref="RisingTideData.AwardPoints" />, the same currency the reward store spends.
///     Cargo reaches players from <c>PlunderBeacon</c> kills (High Seas plunder). <c>SetWearable</c> becomes
///     <c>AddItem</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackMarketMerchant : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public BlackMarketMerchant() : base("пират")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public static BlackMarketMerchant InstanceTram { get; set; }

    public static BlackMarketMerchant InstanceFel { get; set; }

    public static Point3D SpawnLocation => new(2719, 2187, 0);

    public override bool IsActiveVendor => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;

        PackItem(new QuartermasterRewardDeed());
        PackItem(new SailingMasterRewardDeed());
        PackItem(new BotswainRewardDeed());
        PackItem(new PowderMonkeyRewardDeed());
        PackItem(new SpikedWhipOfPlundering());
        PackItem(new BladedWhipOfPlundering());
        PackItem(new BarbedWhipOfPlundering());
        PackItem(new TritonStatue());
    }

    public override void InitOutfit()
    {
        AddItem(new ElvenPants { Hue = 1307 });
        AddItem(new Shirt { Hue = 1209 });
        AddItem(new JinBaori { Hue = 2017 });
        AddItem(new Sandals { Hue = 0 });
        AddItem(new Bandana { Hue = 2051 });
        AddItem(new BarbedWhip { Hue = 2721 });
        AddItem(new ShoulderParrot { Hue = 18 });
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1158914); // Maritime Black Market Merchant
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 5) && pm.CheckAlive())
        {
            pm.SendGump(new RisingTideRewardGump(this, pm));
        }
    }

    /// <summary>
    ///     Cargo handed to the merchant is bought for doubloons (ServUO's <c>BaseTurnInMobile</c> turn-in).
    ///     Anything else keeps the vendor's normal drag-and-drop behaviour.
    /// </summary>
    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is MaritimeCargo cargo)
        {
            return TryBuyCargo(from, cargo);
        }

        return base.OnDragDrop(from, dropped);
    }

    /// <summary>Context menu "sell maritime cargo": hands over every cargo item in the backpack at once.</summary>
    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from != null)
        {
            list.Add(new SellCargoEntry(this, from));
        }
    }

    /// <summary>Pays doubloons for one piece of maritime cargo (in ServUO — <c>AwardPoints</c>).</summary>
    public bool TryBuyCargo(Mobile from, MaritimeCargo cargo)
    {
        if (from == null || cargo?.Deleted != false)
        {
            return false;
        }

        if (!from.InRange(Location, 5))
        {
            SayTo(from, 500446); // That is too far away.
            return false;
        }

        if (from.Backpack == null || !cargo.IsChildOf(from.Backpack))
        {
            SayTo(from, 1042001); // That must be in your pack for you to use it.
            return false;
        }

        var doubloons = Math.Max(1, cargo.GetAwardAmount());

        cargo.Delete();

        RisingTideData.AwardPoints(from, doubloons);

        from.SendMessage(0x35, $"Вы продали морской груз: {doubloons} дублонов.");

        SayTo(from, 1158911); // Bring me maritime trade cargo and I will pay you in doubloons!

        return true;
    }

    /// <summary>Sells all cargo the player carries, one item at a time.</summary>
    public int SellAllCargo(Mobile from)
    {
        var pack = from?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var items = pack.Items.ToArray();
        var sold = 0;

        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] is MaritimeCargo cargo && TryBuyCargo(from, cargo))
            {
                sold++;
            }
        }

        if (sold == 0)
        {
            SayTo(from, 1158911); // Bring me maritime trade cargo...
        }

        return sold;
    }

    private sealed class SellCargoEntry : ContextMenuEntry
    {
        private readonly BlackMarketMerchant _merchant;
        private readonly Mobile _from;

        public SellCargoEntry(BlackMarketMerchant merchant, Mobile from) : base(1158912, 5) // Sell Maritime Trade Cargo
        {
            _merchant = merchant;
            _from = from;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_merchant?.Deleted == false)
            {
                _merchant.SellAllCargo(_from);
            }
        }
    }

    public override void Delete()
    {
        base.Delete();

        if (InstanceTram == this)
        {
            InstanceTram = null;
        }
        else if (InstanceFel == this)
        {
            InstanceFel = null;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Map == Map.Trammel)
        {
            InstanceTram = this;
        }
        else if (Map == Map.Felucca)
        {
            InstanceFel = this;
        }
    }
}
