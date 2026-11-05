using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Multis;
using System.Linq;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Craft Addons/CraftAddonDeed.cs).
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class CraftAddonDeed : BaseAddonDeed
{
    public int UsesRemaining { get; set; }

    public CraftAddonDeed(int uses)
    {
        LootType = LootType.Blessed;
        UsesRemaining = uses;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, UsesRemaining.ToString());
    }
}
