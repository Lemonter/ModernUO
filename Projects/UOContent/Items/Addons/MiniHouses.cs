using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/MiniHouses.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MiniHouseAddonComponent : AddonComponent
{
    private MiniHouseType Type { get; set; }

    public override int LabelNumber => ((MiniHouseAddon)Addon).LabelNumber;

    public MiniHouseAddonComponent(int itemID)
        : base(itemID)
    {
    }
}
