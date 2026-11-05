using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/KingsPainting.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KingsPainting1 : BaseDecorationArtifact
{
    public override int ArtifactRarity => 8;

    public override double DefaultWeight => 10.0;

    public override string DefaultName => "A Painting From The Personal Collection Of The King";

    [Constructible]
    public KingsPainting1()
        : base(19552)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class KingsPainting2 : BaseDecorationArtifact
{
    public override int ArtifactRarity => 8;

    public override double DefaultWeight => 10.0;

    public override string DefaultName => "A Painting From The Personal Collection Of The King";

    [Constructible]
    public KingsPainting2()
        : base(19558)
    {
    }
}
