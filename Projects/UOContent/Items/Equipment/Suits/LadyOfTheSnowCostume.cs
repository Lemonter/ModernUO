using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/LadyOfTheSnowCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LadyOfTheSnowCostume : BaseCostume
{
    public override string CreatureName => "lady of the snow";

    [Constructible]
    public LadyOfTheSnowCostume() : base( )
    {
        this.CostumeBody = 252;
    }

    public override int LabelNumber => 1114241; // Lady of the Snow costume
}
