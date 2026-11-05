using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/ConjurersGarbHarbringer.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ConjureresGarbHarbringer : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1114052; // Conjurer's Garb

    [Constructible]
    public ConjureresGarbHarbringer()
        : base(0x1F03, 0x486)
    {
        Hue = 0x4AA;
        Weight = 3.0;
        Attributes.DefendChance = 5;
        Attributes.RegenMana = 2;
    }
}
