using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/BloodwormCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BloodwormCostume : BaseCostume
{
    public override string CreatureName => "bloodworm";

    [Constructible]
    public BloodwormCostume() : base( )
    {
        this.CostumeBody = 287;
    }

    public override int LabelNumber => 1114006; // bloodworm halloween costume
}
