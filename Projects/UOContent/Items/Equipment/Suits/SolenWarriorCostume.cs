using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/SolenWarriorCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SolenWarriorCostume : BaseCostume
{
    public override string CreatureName => "solen warrior";

    [Constructible]
    public SolenWarriorCostume() : base( )
    {
        this.CostumeBody = 782;
    }

    public override int LabelNumber => 1114231; // solen warrior costume
}
