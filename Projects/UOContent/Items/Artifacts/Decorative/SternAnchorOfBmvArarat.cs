using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/SternAnchorOfBmvArarat.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SternAnchorOfBmvArarat : BaseDecorationArtifact
{
    public override int ArtifactRarity => 8;

    [Constructible]
    public SternAnchorOfBmvArarat()
        : base(0x14F7)
    {
        this.Name = "кормовой якорь «Арарата»";
        this.Weight = 10.0;
        this.Hue = 2959; // checked
    }
}
