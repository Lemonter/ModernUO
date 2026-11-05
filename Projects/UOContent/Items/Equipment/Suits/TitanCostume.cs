using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/TitanCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TitanCostume : BaseCostume
{
    public override string CreatureName => "titan";

    [Constructible]
    public TitanCostume() : base( )
    {
        this.CostumeBody = 76;
    }

    public override int LabelNumber => 1114238; // titan costume
}
