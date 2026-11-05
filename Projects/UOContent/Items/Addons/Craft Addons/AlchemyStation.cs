using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/AlchemyStation.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AlchemyStation : CraftAddon
{
    public override BaseAddonDeed Deed => new AlchemyStationDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0);
    public override CraftSystem CraftSystem => DefAlchemy.CraftSystem;

    public AlchemyStation(bool south, int uses)
    {
        if (south)
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 40323, 40324, 1157070, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40332, 1157070), 1, 0, 0);
        }
        else
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 40334, 40335, 1157070, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40343, 1157070), 0, -1, 0);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class AlchemyStationDeed : CraftAddonDeed
{
    public override int LabelNumber => 1157070; // Alchemy Station
    public override BaseAddon Addon => new AlchemyStation(_South, UsesRemaining);

    private bool _South;

    [Constructible]
    public AlchemyStationDeed() : this(0)
    {
    }

    public AlchemyStationDeed(int uses) : base(uses)
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
