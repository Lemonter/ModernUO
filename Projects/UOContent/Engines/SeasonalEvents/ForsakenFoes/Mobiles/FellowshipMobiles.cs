using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Mobile/FellowshipAdept.cs).
///     <c>SetWearable</c> becomes <c>AddItem</c>; the context menu uses this codebase's
///     <c>ref PooledRefList&lt;ContextMenuEntry&gt;</c> signature.
/// </summary>
[SerializationGenerator(0, false)]
public partial class FellowshipAdept : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public FellowshipAdept() : base("адепт Fellowship")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public static FellowshipAdept InstanceTram { get; set; }

    public static FellowshipAdept InstanceFel { get; set; }

    public override bool IsActiveVendor => false;

    public override bool IsInvulnerable => true;

    public override bool DisallowAllMoves => true;

    public override bool ClickTitle => true;

    public override bool CanTeach => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");

        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
        HairItemID = 0x2044;
        HairHue = 1644;
        FacialHairItemID = 0x203F;
        FacialHairHue = 1644;
    }

    public override void InitOutfit()
    {
        AddItem(new Kamishimo());
        AddItem(new Sandals());
        AddItem(new GoldRing());

        if (Backpack == null)
        {
            AddItem(new Backpack { Movable = false });
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1159182); // Fellowship Shop
    }

    public override void AddCustomContextEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        if (from.Alive)
        {
            list.Add(new BrowseShopEntry(from, this));
        }

        base.AddCustomContextEntries(from, ref list);
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

    private class BrowseShopEntry : ContextMenuEntry
    {
        private readonly Mobile _from;
        private readonly BaseVendor _vendor;

        public BrowseShopEntry(Mobile from, BaseVendor vendor) : base(1159181, 2) // Browse the Fellowship Shop
        {
            Enabled = vendor.CheckVendorAccess(from);

            _from = from;
            _vendor = vendor;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!_from.InRange(_vendor.Location, 5) || _from is not PlayerMobile pm)
            {
                return;
            }

            pm.SendGump(new FellowshipRewardGump(_vendor, pm));
        }
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Mobile/Follower.cs).
///     ServUO's <c>BaseQuester</c> does not exist here, so the follower is a non-selling <see cref="BaseVendor" />
///     that shows its gump on double-click instead of on talk.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Follower : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Follower() : base("последователь")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public static Follower InstanceTram { get; set; }

    public static Follower InstanceFel { get; set; }

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
    }

    public override void InitOutfit()
    {
        AddItem(new Surcoat { Hue = 1255 });
        AddItem(new LongPants { Hue = 2722 });
        AddItem(new Kilt { Hue = 2012 });
        AddItem(new Boots());
        AddItem(new GoldEarrings());
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(Location, 5))
        {
            from.SendGump(new FollowerGump());
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

    public class FollowerGump : Gumps.Gump
    {
        public FollowerGump() : base(100, 100)
        {
            AddPage(0);

            AddBackground(0, 0, 720, 285, 0x2454);
            AddImage(0, 0, 0x6CF);
            AddHtmlLocalized(300, 24, 408, 251, 1159043, 0xC63, false, true);
        }
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Mobile/Worker.cs).
///     ServUO's file-backed <c>Persistence.Serialize</c> state is persisted through
///     <see cref="FellowshipData" />'s generic persistence instead. <c>SetWearable</c> becomes <c>AddItem</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Worker : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private FellowshipChain _chain;

    [Constructible]
    public Worker(FellowshipChain chain) : base("рабочий")
    {
        _chain = chain;
    }

    [Constructible]
    public Worker() : this(FellowshipChain.None)
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;

    public override void InitSBInfo()
    {
    }

    public static Worker InstanceTram { get; set; }

    public static Worker InstanceFel { get; set; }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new FullApron { Hue = 902 });
        AddItem(new HalfApron { Hue = 946 });
        AddItem(new Shirt { Hue = 1513 });
        AddItem(new LeatherGloves());
        AddItem(new Boots { Hue = 2013 });
        AddItem(new ShortPants());
        AddItem(new SmithHammer());
    }

    public override bool OnDragDrop(Mobile from, Item item)
    {
        if (item is not FellowshipCoin)
        {
            SayTo(from, 500607, 0x3B2); // I'm not interested in that.
            return false;
        }

        if (FellowshipData.TryGetChain(from, out var chain))
        {
            if (_chain <= chain)
            {
                SayTo(from, 500607, 0x3B2); // I'm not interested in that.
                return false;
            }

            FellowshipData.SetChain(from, _chain);

            if (_chain == FellowshipChain.Eight)
            {
                from.AddToBackpack(new FellowshipMedallion());
            }
        }
        else
        {
            FellowshipData.SetChain(from, _chain);
        }

        item.Delete();

        return true;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m != null && InRange(m.Location, 5))
        {
            m.SendGump(new WorkerGump(m, _chain));
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

    public class WorkerGump : Gumps.Gump
    {
        private static readonly int[,] _clilocs =
        {
            { 1159238, 1159239 },
            { 1159236, 1159240 },
            { 1159236, 1159241 },
            { 1159236, 1159242 },
            { 1159236, 1159243 },
            { 1159236, 1159244 },
            { 1159236, 1159245 },
            { 1159236, 1159246 }
        };

        public WorkerGump(Mobile from, FellowshipChain chain) : base(100, 100)
        {
            if (chain == FellowshipChain.None)
            {
                chain = FellowshipChain.One;
            }

            int cliloc;

            if (FellowshipData.TryGetChain(from, out var current))
            {
                cliloc = chain > current ? _clilocs[(int)chain - 1, 0] : _clilocs[(int)chain - 1, 1];
            }
            else
            {
                cliloc = _clilocs[(int)chain - 1, 0];
            }

            AddPage(0);

            AddBackground(0, 0, 620, 328, 0x2454);
            AddImage(0, 0, 0x61A);
            AddHtmlLocalized(335, 14, 273, 18, 1114513, "#1159237", 0xC63, false, false);
            AddHtmlLocalized(335, 51, 273, 267, cliloc, 0xC63, false, true);
        }
    }
}
