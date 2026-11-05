using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/OphidianWarriorCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OphidianWarriorCostume : BaseCostume
{
    public override string CreatureName => "ophidian warrior";

    [Constructible]
    public OphidianWarriorCostume() : base( )
    {
        this.CostumeBody = 86;
    }

    public override int LabelNumber => 1114229; // ophidian warrior costume
}
