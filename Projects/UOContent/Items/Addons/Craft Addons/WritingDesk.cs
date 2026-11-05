using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/WritingDesk.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WritingDesk : CraftAddon
{
    public override BaseAddonDeed Deed => new WritingDeskDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0);
    public override CraftSystem CraftSystem => DefInscription.CraftSystem;

    public WritingDesk(bool south, int uses)
    {
        if (south)
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 40938, 40939, 1124962, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40953, 1124962), 1, 0, 0);
        }
        else
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 40945, 40946, 1124962, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40952, 1124962), 0, -1, 0);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class WritingDeskDeed : CraftAddonDeed
{
    public override int LabelNumber => 1157989; // Enchanted Writing Desk
    public override BaseAddon Addon => new WritingDesk(_South, UsesRemaining);

    private bool _South;

    [Constructible]
    public WritingDeskDeed() : this(0)
    {
    }

    public WritingDeskDeed(int uses) : base(uses)
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
