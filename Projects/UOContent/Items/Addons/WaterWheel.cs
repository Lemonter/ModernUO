using ModernUO.Serialization;
using System;
using Server.Engines.VeteranRewards;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WaterWheel.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WaterWheelAddon : BaseAddon, IWaterSource
{
    public WaterWheelAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0xA0F8, 1125222), 0, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0xA0EE, 1125222), 0, 0, 0);
                break;
        }
    }

    public int Quantity
    {
        get { return 500; }
        set {}
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem { get; set; }

    public override BaseAddonDeed Deed
    {
        get
        {
            WaterWheelDeed deed = new WaterWheelDeed();
            deed.IsRewardItem = IsRewardItem;

            return deed;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class WaterWheelDeed : BaseAddonDeed, IRewardItem, IRewardOption
{
    public override int LabelNumber => 1158881; // Water Wheel

    public override BaseAddon Addon
    {
        get
        {
            WaterWheelAddon addon = new WaterWheelAddon(_direction);
            addon.IsRewardItem = _isRewardItem;

            return addon;
        }
    }

    [SerializableField(0)]
    private DirectionType _direction;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public WaterWheelDeed()
        : base()
    {
        LootType = LootType.Blessed;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
            list.Add(1080457); // 10th Year Veteran Reward
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)DirectionType.South, 1075386); // South
        list.Add((int)DirectionType.East, 1075387); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _direction = (DirectionType)choice;

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
