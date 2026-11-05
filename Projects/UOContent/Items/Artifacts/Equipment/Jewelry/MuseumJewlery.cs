using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/MuseumJewlery.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VesperCollectionRing : GoldRing
{
    [Constructible]
    public VesperCollectionRing()
        : base()
    {
    }

    public override int LabelNumber => 1073234; // A Souvenir from the Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class VesperCollectionNecklace : GoldNecklace
{
    [Constructible]
    public VesperCollectionNecklace()
        : base()
    {
    }

    public override int LabelNumber => 1073234; // A Souvenir from the Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class VesperCollectionBracelet : GoldBracelet
{
    [Constructible]
    public VesperCollectionBracelet()
        : base()
    {
    }

    public override int LabelNumber => 1073234; // A Souvenir from the Museum of Vesper
}

[SerializationGenerator(0, false)]
public partial class VesperCollectionEarrings : GoldEarrings
{
    [Constructible]
    public VesperCollectionEarrings()
        : base()
    {
    }

    public override int LabelNumber => 1073234; // A Souvenir from the Museum of Vesper
}
