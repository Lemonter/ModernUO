using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/CorporealBrumeStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CorporealBrumeStatuette : ShimmeringEffusionStatuette
{
    [Constructible]
    public CorporealBrumeStatuette()
        : base(0x2D94)
    {
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1074506; // Corporeal Brume Statuette
}
