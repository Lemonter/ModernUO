using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/ShadowWyrmCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShadowWyrmCostume : BaseCostume
{
    public override string CreatureName => "shadow wyrm";

    [Constructible]
    public ShadowWyrmCostume() : base( )
    {
        CostumeBody = 106;
        CostumeHue = 0;
    }

    public override int LabelNumber => 1114009; // shadow wyrm halloween costume
}
