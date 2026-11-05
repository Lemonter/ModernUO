using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/DreamWraithCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DreamWraithCostume : BaseCostume
{
    public override string CreatureName => "dream wraith";

    [Constructible]
    public DreamWraithCostume() : base( )
    {
        this.CostumeBody = 740;
    }

    public override int LabelNumber => 1114008; // dream wraith halloween costume
}
