using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/GiantPixieCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GiantPixieCostume : BaseCostume
{
    public override string CreatureName => "giant pixie";

    [Constructible]
    public GiantPixieCostume() : base( )
    {
        this.CostumeBody = 176;
    }

    public override int LabelNumber => 1114244; // giant pixie costume
}
