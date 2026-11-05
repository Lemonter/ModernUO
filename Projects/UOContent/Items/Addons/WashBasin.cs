using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WashBasin.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WaterContainerComponent : AddonComponent, IWaterSource
{
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int Quantity
    {
        get => _quantity;
        set
        {
            if (value != _quantity)
            {
                _quantity = value < 1 ? 0 : value > _maxQuantity ? _maxQuantity : value;

                ItemID = IsEmpty ? _itemId : _fullItemId;
            }
        }
    }

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _itemId;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _fullItemId;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxQuantity;

    public int Item_ID
    {
        get => _itemId;
        set => _itemId = value;
    }

    public int FullItem_ID
    {
        get => _fullItemId;
        set => _fullItemId = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual bool IsEmpty => _quantity <= 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual bool IsFull => _quantity >= _maxQuantity;

    public WaterContainerComponent(int itemID, int fullitemid, int maxquantity)
        : base(itemID)
    {
        _maxQuantity = maxquantity;
        _itemId = itemID;
        _fullItemId = fullitemid;
    }

    public override void AddNameProperties(IPropertyList list)
    {
        list.Add(1049522, "#1115912"); // a container with ~1_DRINK_NAME~
    }
}

[SerializationGenerator(0, false)]
public partial class WashBasinAddon : BaseAddon
{
    public WashBasinAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(41646, 1125668), 1, 0, 0);
                AddComponent(new WaterContainerComponent(41645, 41647, 5), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(41644, 1125668), -1, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(41653, 1125668), 0, -1, 0);
                AddComponent(new WaterContainerComponent(41652, 41654, 5), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(41651, 1125668), 0, 1, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new WashBasinDeed();
}

[SerializationGenerator(0, false)]
public partial class WashBasinDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1158966; // Wash Basin

    public override BaseAddon Addon
    {
        get
        {
            WashBasinAddon addon = new WashBasinAddon(_Direction);

            return addon;
        }
    }

    private DirectionType _Direction;

    [Constructible]
    public WashBasinDeed()
        : base()
    {
        LootType = LootType.Blessed;
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)DirectionType.South, 1075386); // South
        list.Add((int)DirectionType.East, 1075387); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _Direction = (DirectionType)choice;

        if (!Deleted)
            base.OnDoubleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            RewardOptionGump.DisplayTo(from, this, 1154194); // Choose a Facing:
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}
