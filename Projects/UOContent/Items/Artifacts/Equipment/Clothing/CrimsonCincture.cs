using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/CrimsonCincture.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishCrimsonCincture : GargoyleHalfApron
{
    [Constructible]
    public GargishCrimsonCincture()
        : base()
    {
        Hue = 0x485;
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
    }

    public override int LabelNumber => 1075043; // Crimson Cincture
}
