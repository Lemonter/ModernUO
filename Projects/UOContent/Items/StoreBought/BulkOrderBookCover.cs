using ModernUO.Serialization;
using Server.Engines.BulkOrders;
using Server.Targeting;

namespace Server.Items;

public enum CoverType
{
    Normal,
    DullCopper,
    ShadowIron,
    Copper,
    Bronze,
    Gold,
    Agapite,
    Verite,
    Valorite,
    Spined,
    Horned,
    Barbed,
    Oak,
    Ash,
    Yew,
    Heartwood,
    Bloodwood,
    Frostwood,
    Alchemy,
    Blacksmith,
    Cooking,
    Fletching,
    Carpentry,
    Inscription,
    Tailoring,
    Tinkering
}

public class CoverInfo
{
    public static CoverInfo[] Infos { get; } =
    [
        new(CoverType.Normal, 1071097, 0),
        new(CoverType.DullCopper, 1071101, CraftResources.GetHue(CraftResource.DullCopper)),
        new(CoverType.ShadowIron, 1071107, CraftResources.GetHue(CraftResource.ShadowIron)),
        new(CoverType.Copper, 1071108, CraftResources.GetHue(CraftResource.Copper)),
        new(CoverType.Bronze, 1071109, CraftResources.GetHue(CraftResource.Bronze)),
        new(CoverType.Gold, 1071112, CraftResources.GetHue(CraftResource.Gold)),
        new(CoverType.Agapite, 1071113, CraftResources.GetHue(CraftResource.Agapite)),
        new(CoverType.Verite, 1071114, CraftResources.GetHue(CraftResource.Verite)),
        new(CoverType.Valorite, 1071115, CraftResources.GetHue(CraftResource.Valorite)),
        new(CoverType.Spined, 1071098, CraftResources.GetHue(CraftResource.SpinedLeather)),
        new(CoverType.Horned, 1071099, CraftResources.GetHue(CraftResource.HornedLeather)),
        new(CoverType.Barbed, 1071100, CraftResources.GetHue(CraftResource.BarbedLeather)),
        new(CoverType.Oak, 1071410, CraftResources.GetHue(CraftResource.OakWood)),
        new(CoverType.Ash, 1071411, CraftResources.GetHue(CraftResource.AshWood)),
        new(CoverType.Yew, 1071412, CraftResources.GetHue(CraftResource.YewWood)),
        new(CoverType.Heartwood, 1071413, CraftResources.GetHue(CraftResource.Heartwood)),
        new(CoverType.Bloodwood, 1071414, CraftResources.GetHue(CraftResource.Bloodwood)),
        new(CoverType.Frostwood, 1071415, CraftResources.GetHue(CraftResource.Frostwood)),
        new(CoverType.Alchemy, 1157605, 2505, 1002000),
        new(CoverType.Blacksmith, 1157605, 0x44E, 1157607),
        new(CoverType.Cooking, 1157605, 1169, 1002063),
        new(CoverType.Fletching, 1157605, 1425, 1002047),
        new(CoverType.Carpentry, 1157605, 1512, 1002054),
        new(CoverType.Inscription, 1157605, 2598, 1002090),
        new(CoverType.Tailoring, 1157605, 0x483, 1002155),
        new(CoverType.Tinkering, 1157605, 1109, 1002162),
    ];

    public CoverInfo(CoverType type, TextDefinition label, int hue, TextDefinition args = null)
    {
        Type = type;
        Label = label;
        Hue = hue;
        Args = args;
    }

    public CoverType Type { get; }
    public TextDefinition Label { get; }
    public int Hue { get; }
    public TextDefinition Args { get; }

    public static CoverInfo Find(CoverType type)
    {
        foreach (var info in Infos)
        {
            if (info.Type == type)
            {
                return info;
            }
        }

        return null;
    }
}

[SerializationGenerator(0, false)]
public partial class BulkOrderBookCover : Item
{
    [SerializableField(0, fieldChanged: nameof(OnCoverTypeChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CoverType _coverType;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private int _usesRemaining;

    [Constructible]
    public BulkOrderBookCover(CoverType type = CoverType.Normal, int uses = 30) : base(0x2831)
    {
        _usesRemaining = uses;
        LootType = LootType.Blessed;
        _coverType = type;
        InvalidateHue();
    }

    private void OnCoverTypeChanged(CoverType oldValue, CoverType newValue)
    {
        InvalidateHue();
        InvalidateProperties();
    }

    [AfterDeserialization]
    private void AfterDeserialization() => InvalidateHue();

    public void InvalidateHue()
    {
        var info = CoverInfo.Find(_coverType);

        if (info != null)
        {
            Hue = info.Hue;
        }
    }

    public override void AddNameProperty(IPropertyList list)
    {
        var info = CoverInfo.Find(_coverType);

        if (info == null)
        {
            list.Add(1071097); // Bulk Order Cover (Normal)
            return;
        }

        if (info.Args != null)
        {
            var args = info.Args.GetValue();
            list.Add(1157605, args); // Bulk Order Cover (~1_HUE~)
        }
        else if (info.Label.Number > 0)
        {
            list.Add(info.Label.Number);
        }
        else
        {
            list.Add(1114057, info.Label.String); // ~1_val~
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1071121); // Select the bulk order book you want to replace a cover.
            from.Target = new InternalTarget(this);
        }
    }

    private class InternalTarget : Target
    {
        private readonly BulkOrderBookCover _cover;

        public InternalTarget(BulkOrderBookCover cover) : base(-1, false, TargetFlags.None) => _cover = cover;

        protected override void OnTarget(Mobile m, object targeted)
        {
            if (_cover.Deleted)
            {
                return;
            }

            if (targeted is not BulkOrderBook bob)
            {
                m.SendLocalizedMessage(1071118); // You can only cover a bulk order book with this item.
                return;
            }

            if (!bob.IsChildOf(m.Backpack))
            {
                m.SendLocalizedMessage(1071117); // You cannot use this item for it.
                return;
            }

            if (bob.Hue == _cover.Hue)
            {
                m.SendLocalizedMessage(1071122); // You cannot cover it with same color.
                return;
            }

            bob.Hue = _cover.Hue;
            _cover.UsesRemaining -= 1;

            // You have successfully given the bulk order book a new cover.
            m.SendLocalizedMessage(1071119);
            m.PlaySound(0x048);

            if (_cover.UsesRemaining <= 0)
            {
                m.SendLocalizedMessage(1071120); // You have used up all the bulk order book covers.
                _cover.Delete();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class BagOfBulkOrderCovers : Bag
{
    public override int LabelNumber => 1071116; // Bag of bulk order covers

    [Constructible]
    public BagOfBulkOrderCovers()
    {
    }

    public BagOfBulkOrderCovers(int start, int end)
    {
        for (var i = start; i <= end; i++)
        {
            if (i >= 0 && i < CoverInfo.Infos.Length)
            {
                DropItem(new BulkOrderBookCover(CoverInfo.Infos[i].Type));
            }
        }
    }
}
