using ModernUO.Serialization;
using System;
using Server.Engines.Craft;
using Server.Engines.VeteranRewards;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/EnchantedSculptingTool.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class EnchantedSculptingToolAddon : CraftAddon
{
    public override CraftSystem CraftSystem => DefMasonry.CraftSystem;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsRewardItem { get; set; }

    public override BaseAddonDeed Deed
    {
        get
        {
            EnchantedSculptingToolDeed deed = new EnchantedSculptingToolDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0)
            {
                IsRewardItem = IsRewardItem
            };

            return deed;
        }
    }

    public EnchantedSculptingToolAddon(DirectionType type, int uses)
    {
        switch (type)
        {
            case DirectionType.South:
                AddCraftComponent(new AddonToolComponent(CraftSystem, 0xA538, 0xA541, 1157988, 1157987, 1029241, uses, this), 0, 0, 0);
                AddComponent(new ToolDropComponent(0xA549, 1029241), 0, 1, 0);
                break;
            case DirectionType.East:
                AddCraftComponent(new AddonToolComponent(CraftSystem, 0xA540, 0xA539, 1157988, 1157987, 1029241, uses, this), 0, 0, 0);
                AddComponent(new ToolDropComponent(0xA548, 1029241), 1, 0, 0);
                break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class EnchantedSculptingToolDeed : CraftAddonDeed, IRewardItem, IRewardOption
{
    public override int LabelNumber => 1159421; // Enchanted Sculpting Tool

    public override BaseAddon Addon
    {
        get
        {
            EnchantedSculptingToolAddon addon = new EnchantedSculptingToolAddon(_direction, UsesRemaining)
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
    public EnchantedSculptingToolDeed()
        : this(0)
    {
    }

    public EnchantedSculptingToolDeed(int uses)
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
