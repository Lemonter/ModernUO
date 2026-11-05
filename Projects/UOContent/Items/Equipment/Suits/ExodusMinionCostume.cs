using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/ExodusMinionCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ExodusMinionCostume : BaseCostume
{
    public override string CreatureName => "exodus minion";

    [Constructible]
    public ExodusMinionCostume() : base( )
    {
        this.CostumeBody = 757;
    }

    public override int LabelNumber => 1114239; // exodus minion costume
}
