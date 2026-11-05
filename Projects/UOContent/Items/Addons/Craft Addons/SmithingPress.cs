using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/SmithingPress.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SmithingPress : CraftAddon
{
    public override BaseAddonDeed Deed => new SmithingPressDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0);
    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;

    public SmithingPress(bool south, int uses)
    {
        if (south)
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 39592, 39553, 1123577, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(39569, 1123593), -1, 0, 0);
        }
        else
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 39593, 39561, 1123577, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(39569, 1123593), 0, 1, 0);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class SmithingPressDeed : CraftAddonDeed
{
    public override int LabelNumber => 1123577; // smithing press
    public override BaseAddon Addon => new SmithingPress(_South, UsesRemaining);

    private bool _South;

    [Constructible]
    public SmithingPressDeed() : this(0)
    {
    }

    public SmithingPressDeed(int uses) : base(uses)
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
