using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/EtherealWarriorCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class EtherealWarriorCostume : BaseCostume
{
    public override string CreatureName => "ethereal warrior";

    [Constructible]
    public EtherealWarriorCostume() : base( )
    {
        this.CostumeBody = 123;
    }

    public override int LabelNumber => 1114243; // ethereal warrior costume
}
