using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/CentaurCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CentaurCostume : BaseCostume
{
    public override string CreatureName => "centaur";

    [Constructible]
    public CentaurCostume() : base()
    {
        this.CostumeBody = 101;
    }

    public override int LabelNumber => 1114235; // centaur costume
}
