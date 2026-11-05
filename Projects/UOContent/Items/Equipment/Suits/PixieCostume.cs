using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/PixieCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PixieCostume : BaseCostume
{
    public override string CreatureName => "pixie";

    [Constructible]
    public PixieCostume() : base( )
    {
        this.CostumeBody = 128;
    }

    public override int LabelNumber => 1114236; // pixie costume
}
