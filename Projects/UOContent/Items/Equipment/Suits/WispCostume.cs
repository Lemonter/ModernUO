using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/WispCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WispCostume : BaseCostume
{
    public override string CreatureName => "wisp";

    [Constructible]
    public WispCostume() : base( )
    {
        this.CostumeBody = 58;
    }

    public override int LabelNumber => 1114225; // wisp costume
}
