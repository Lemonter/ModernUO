using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/MantraEffervescenceStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MantraEffervescenceStatuette : ShimmeringEffusionStatuette
{
    [Constructible]
    public MantraEffervescenceStatuette()
        : base(0x2D94)
    {
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1074505; // Mantra Effervescence Statuette
}
