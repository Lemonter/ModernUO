using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/GoreFiendCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GoreFiendCostume : BaseCostume
{
    public override string CreatureName => "gore fiend";

    [Constructible]
    public GoreFiendCostume() : base( )
    {
        this.CostumeBody = 305;
    }

    public override int LabelNumber => 1114227; // gore fiend costume
}
