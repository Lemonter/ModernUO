using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/SatyrCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SatyrCostume : BaseCostume
{
    public override string CreatureName => "satyr";

    [Constructible]
    public SatyrCostume() : base( )
    {
        this.CostumeBody = 271;
    }

    public override int LabelNumber => 1114287; // satyr costume
}
