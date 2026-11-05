using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/MongbatCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MongbatCostume : BaseCostume
{
    public override string CreatureName => "mongbat";

    [Constructible]
    public MongbatCostume() : base( )
    {
        this.CostumeBody = 39;
    }

    public override int LabelNumber => 1114223; // mongbat costume
}
