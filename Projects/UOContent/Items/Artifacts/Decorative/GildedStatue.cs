using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/GildedStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KingsGildedStatue : BaseDecorationArtifact
{
    public override int ArtifactRarity => 8;

    public override double DefaultWeight => 10.0;

    public override string DefaultName => "A Gilded Statue from the Personal Collection of the King";

    [Constructible]
    public KingsGildedStatue()
        : base(0x139D)
    {
        Hue = 2721;
    }
}
