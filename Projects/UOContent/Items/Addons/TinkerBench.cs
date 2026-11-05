using ModernUO.Serialization;
using System;
using Server.Engines.Craft;
using Server.Engines.VeteranRewards;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TinkerBench.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TinkerBenchAddon : CraftAddon
{
    public override CraftSystem CraftSystem => DefTinkering.CraftSystem;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem { get; set; }

    public override BaseAddonDeed Deed
    {
        get
        {
            TinkerBenchDeed deed = new TinkerBenchDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0);
            deed.IsRewardItem = IsRewardItem;

            return deed;
        }
    }

    public TinkerBenchAddon(DirectionType type, int uses)
    {
        switch (type)
        {
            case DirectionType.South:
                AddCraftComponent(new AddonToolComponent(CraftSystem, 0xA213, 0xA212, 1157072, 1157073, 1125529, uses, this), 0, 0, 0);
                AddComponent(new ToolDropComponent(41489, 1015078), 0, 1, 0);
                break;
            case DirectionType.East:
                AddCraftComponent(new AddonToolComponent(CraftSystem, 0xA21B, 0xA21A, 1157072, 1157073, 1125529, uses, this), 0, 0, 0);
                AddComponent(new ToolDropComponent(41489, 1015078), 1, 0, 0);
                break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class TinkerBenchDeed : CraftAddonDeed, IRewardItem, IRewardOption
{
    public override int LabelNumber => 1125529; // tinker bench

    public override BaseAddon Addon
    {
        get
        {
            TinkerBenchAddon addon = new TinkerBenchAddon(_direction, UsesRemaining);
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
    public TinkerBenchDeed()
        : this(0)
    {
    }

    public TinkerBenchDeed(int uses)
        : base(uses)
    {
        LootType = LootType.Blessed;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
            list.Add(1076223); // 7th Year Veteran Reward
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
