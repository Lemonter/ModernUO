using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/OphidianMatriarchCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OphidianMatriarchCostume : BaseCostume
{
    public override string CreatureName => "ophidian matriarch";

    [Constructible]
    public OphidianMatriarchCostume() : base( )
    {
        this.CostumeBody = 87;
    }

    public override int LabelNumber => 1114230; // ophidian matriarch costume
}
