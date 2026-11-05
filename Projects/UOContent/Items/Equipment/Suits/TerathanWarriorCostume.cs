using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/TerathanWarriorCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TerathanWarriorCostume : BaseCostume
{
    public override string CreatureName => "terathan warrior";

    [Constructible]
    public TerathanWarriorCostume() : base( )
    {
        this.CostumeBody = 70;
    }

    public override int LabelNumber => 1114228; // terathan warrior costume
}
