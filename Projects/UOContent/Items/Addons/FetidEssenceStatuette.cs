using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FetidEssenceStatuette.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FetidEssenceStatuette : ShimmeringEffusionStatuette
{
    [Constructible]
    public FetidEssenceStatuette()
        : base(0x2D94)
    {
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1074504; // Fetid Essence Statuette
}
