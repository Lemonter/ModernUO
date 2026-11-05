using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/FireElementalCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FireElementalCostume : BaseCostume
{
    public override string CreatureName => "fire elemental";

    [Constructible]
    public FireElementalCostume() : base( )
    {
        this.CostumeBody = 15;
    }

    public override int LabelNumber => 1114224; // fire elemental costume
}
