using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/SkitteringHopperCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SkitteringHopperCostume : BaseCostume
{
    public override string CreatureName => "skittering hopper";

    [Constructible]
    public SkitteringHopperCostume() : base( )
    {
        this.CostumeBody = 302;
    }

    public override int LabelNumber => 1114240; // skittering hopper costume
}
