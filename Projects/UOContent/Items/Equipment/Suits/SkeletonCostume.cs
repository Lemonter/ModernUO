using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/SkeletonCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SkeletonCostume : BaseCostume
{
    public override string CreatureName => "skeleton";

    [Constructible]
    public SkeletonCostume() : base( )
    {
        this.CostumeBody = 50;
    }

    public override int LabelNumber => 1113996; // skeleton halloween costume
}
