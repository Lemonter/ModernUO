using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/WolfSpiderCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WolfSpiderCostume : BaseCostume
{
    public override string CreatureName => "wolf spider";

    [Constructible]
    public WolfSpiderCostume() : base( )
    {
        CostumeBody = 736;
    }

    public override int LabelNumber => 1114232; // wolf spider costume
}
