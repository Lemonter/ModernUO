using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/VoidWandererCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VoidWandererCostume : BaseCostume
{
    public override string CreatureName => "wanderer of the void";

    [Constructible]
    public VoidWandererCostume() : base( )
    {
        this.CostumeBody = 316;
    }

    public override int LabelNumber => 1114286; // void wanderer costume
}
