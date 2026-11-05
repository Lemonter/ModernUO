using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/GiantToadCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GiantToadCostume : BaseCostume
{
    public override string CreatureName => "giant toad";

    [Constructible]
    public GiantToadCostume() : base( )
    {
        this.CostumeBody = 80;
    }

    public override int LabelNumber => 1114226; // giant toad costume
}
