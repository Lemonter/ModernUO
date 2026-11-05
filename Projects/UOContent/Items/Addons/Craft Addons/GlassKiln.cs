using ModernUO.Serialization;
using System;
using Server.Engines.Craft;
using Server.Engines.VeteranRewards;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/GlassKiln.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GlassKilnAddon : CraftAddon
{
    public override CraftSystem CraftSystem => DefGlassblowing.CraftSystem;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem { get; set; }

    public override BaseAddonDeed Deed
    {
        get
        {
            GlassKilnDeed deed = new GlassKilnDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0)
            {
                IsRewardItem = IsRewardItem
            };

            return deed;
        }
    }

    public GlassKilnAddon(DirectionType type, int uses)
    {
        switch (type)
        {
            case DirectionType.South:
                AddCraftComponent(new AddonToolComponent(CraftSystem, 0xA530, 0xA531, 1157072, 1157073, 1126312, uses, this), 0, 0, 0);
                AddComponent(new ToolDropComponent(0xA52E, 1015081), 0, 1, 0);
                break;
            case DirectionType.East:
                AddCraftComponent(new AddonToolComponent(CraftSystem, 0xA534, 0xA535, 1157072, 1157073, 1126312, uses, this), 0, 0, 0);
                AddComponent(new ToolDropComponent(0xA52F, 1015081), 1, 0, 0);
                break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GlassKilnDeed : CraftAddonDeed, IRewardItem, IRewardOption
{
    public override int LabelNumber => 1159420; // Glass Kiln

    public override BaseAddon Addon
    {
        get
        {
            GlassKilnAddon addon = new GlassKilnAddon(_direction, UsesRemaining)
            {
                IsRewardItem = _isRewardItem
            };

            return addon;
        }
    }

    [SerializableField(0)]
    private DirectionType _direction;



    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public GlassKilnDeed()
        : this(0)
    {
    }

    public GlassKilnDeed(int uses)
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
