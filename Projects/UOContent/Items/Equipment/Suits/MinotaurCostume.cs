using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/MinotaurCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MinotaurCostume : BaseCostume
{
    public override string CreatureName => "minotaur";

    [Constructible]
    public MinotaurCostume() : base( )
    {
        this.CostumeBody = 263;
    }

    public override int LabelNumber => 1114237; // minotaur costume
}
