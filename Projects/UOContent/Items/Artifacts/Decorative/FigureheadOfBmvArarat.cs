using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/FigureheadOfBmvArarat.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FigureheadOfBmvArarat : BaseDecorationArtifact
{
    public override int ArtifactRarity => 8;

    [Constructible]
    public FigureheadOfBmvArarat()
        : base(0x2D0E)
    {
        this.Name = "носовое украшение «Арарата»";
        this.Weight = 10.0;
        this.Hue = 2968; // checked
    }
}
