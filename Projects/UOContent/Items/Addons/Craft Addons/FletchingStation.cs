using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/FletchingStation.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FletchingStation : CraftAddon
{
    public override BaseAddonDeed Deed => new FletchingStationDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0);
    public override CraftSystem CraftSystem => DefBowFletching.CraftSystem;

    public FletchingStation(bool south, int uses)
    {
        if (south)
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 39982, 39983, 1124006, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40004, 1124006), -1, 0, 0);
        }
        else
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 39992, 39993, 1124006, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40003, 1124006), 1, 0, 0);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class FletchingStationDeed : CraftAddonDeed
{
    public override int LabelNumber => 1156370; // Fletching Station
    public override BaseAddon Addon => new FletchingStation(_South, UsesRemaining);

    private bool _South;

    [Constructible]
    public FletchingStationDeed() : this(0)
    {
    }

    public FletchingStationDeed(int uses) : base(uses)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(new SouthEastGump(s =>
            {
                _South = s;
                base.OnDoubleClick(from);
            }));
        }
    }
}
