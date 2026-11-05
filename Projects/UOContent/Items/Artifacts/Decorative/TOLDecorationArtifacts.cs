using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/TOLDecorationArtifacts.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StretchedDinosaurHide : BaseDecorationArtifact
{
    public override int ArtifactRarity => 11;

    [Constructible]
    public StretchedDinosaurHide() : base(4202)
    {
        Name = "растянутая шкура динозавра";
        Hue = 2523;
    }
}

[SerializationGenerator(0, false)]
public partial class CarvedMyrmydexGlyph : BaseDecorationArtifact
{
    public override int ArtifactRarity => 11;

    [Constructible]
    public CarvedMyrmydexGlyph() : base(4676)
    {
        Name = "резной глиф мирмидекса";
        Hue = 2952;
    }
}

[SerializationGenerator(0, false)]
public partial class WakuOnASpit : BaseDecorationArtifact
{
    public override int ArtifactRarity => 11;

    [Constructible]
    public WakuOnASpit() : base(7832)
    {
        Name = "ваку на вертеле";
    }
}

[SerializationGenerator(0, false)]
public partial class SacredLavaRock : BaseDecorationArtifact
{
    public override int ArtifactRarity => 11;

    [Constructible]
    public SacredLavaRock() : base(4962)
    {
        Name = "священная лавовая порода";
        Hue = 1964;
    }
}

[SerializationGenerator(0, false)]
public partial class WhiteTigerFigurine : BaseDecorationArtifact
{
    public override int ArtifactRarity => 11;

    [Constructible]
    public WhiteTigerFigurine() : base(38980)
    {
        Name = "резная фигурка белого тигра";
        Hue = 2500;
    }
}

[SerializationGenerator(0, false)]
public partial class DragonTurtleHatchlingNet : BaseDecorationArtifact
{
    public override int ArtifactRarity => 11;

    [Constructible]
    public DragonTurtleHatchlingNet() : base(3574)
    {
        Name = "сеть для детёныша драконьей черепахи";
    }
}
