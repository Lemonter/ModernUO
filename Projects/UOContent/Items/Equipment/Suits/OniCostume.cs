using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/OniCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OniCostume : BaseCostume
{
    public override string CreatureName => "oni";

    [Constructible]
    public OniCostume() : base( )
    {
        this.CostumeBody = 241;
    }

    public override int LabelNumber => 1114242; // oni costume
}
