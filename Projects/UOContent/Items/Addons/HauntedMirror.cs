using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/HauntedMirror.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HauntedMirrorDeed : BaseAddonDeed
{
    [Constructible]
    public HauntedMirrorDeed()
        : base()
    {
        this.LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new HaunterMirrorAddon();
    public override int LabelNumber => 1074800; // Haunted Mirror
}
