using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/MaddeningHorrorCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MaddeningHorrorCostume : BaseCostume
{
    public override string CreatureName => "maddening horror";

    [Constructible]
    public MaddeningHorrorCostume() : base( )
    {
        this.CostumeBody = 721;
    }

    public override int LabelNumber => 1114233; // maddening horror costume
}
