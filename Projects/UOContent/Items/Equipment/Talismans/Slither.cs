using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Talismans/Slither.cs).
///     Dropped (absent here): IsArtifact.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Slither : BaseTalisman
{
    public override int LabelNumber => 1114782; // Slither

    [Constructible]
    public Slither()
        : base(0x2F5B)
    {
        Hue = 0x587;
        Blessed = RandomTalisman.GetRandomBlessed();
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
        Attributes.DefendChance = 10;
    }
}
