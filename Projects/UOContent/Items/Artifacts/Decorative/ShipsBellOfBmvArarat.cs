using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/ShipsBellOfBmvArarat.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShipsBellOfBmvArarat : BaseDecorationArtifact
{
    public override int ArtifactRarity => 8;

    [Constructible]
    public ShipsBellOfBmvArarat()
        : base(0x4C5E)
    {
        this.Name = "корабельный колокол «Арарата»";
        this.Weight = 10.0;
        this.Hue = 2968; //checked
    }
}
