using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/SpinningLathe.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpinningLathe : CraftAddon
{
    public override BaseAddonDeed Deed => new SpinningLatheDeed(Tools.Count > 0 ? Tools[0].UsesRemaining : 0);
    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

    public SpinningLathe(bool south, int uses)
    {
        if (south)
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 39962, 39963, 1156369, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40006, 1024014), -1, 0, 0);
        }
        else
        {
            AddCraftComponent(new AddonToolComponent(CraftSystem, 39972, 39973, 1156369, uses, this), 0, 0, 0);
            AddComponent(new ToolDropComponent(40007, 1024014), 0, 1, 0);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class SpinningLatheDeed : CraftAddonDeed
{
    public override int LabelNumber => 1156369; // spinning lathe
    public override BaseAddon Addon => new SpinningLathe(_South, UsesRemaining);

    private bool _South;

    [Constructible]
    public SpinningLatheDeed() : this(0)
    {
    }

    public SpinningLatheDeed(int uses) : base(uses)
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
