using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/GazerCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GazerCostume : BaseCostume
{
    public override string CreatureName => "gazer";

    [Constructible]
    public GazerCostume() : base( )
    {
        this.CostumeBody = 22;
    }

    public override int LabelNumber => 1114004; // gazer halloween costume
}
