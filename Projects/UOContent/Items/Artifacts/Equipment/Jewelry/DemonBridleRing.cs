using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/DemonBridleRing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DemonBridleRing : GoldRing
{
    public override int LabelNumber => 1113651; // Demon Bridle Ring

    [Constructible]
    public DemonBridleRing()
    {
        Hue = 39;
        Attributes.CastRecovery = 2;
        Attributes.CastSpeed = 1;
        Attributes.RegenHits = 1;
        Attributes.RegenMana = 1;
        Attributes.DefendChance = 10;
        Attributes.LowerManaCost = 4;
        Resistances.Fire = 5;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
