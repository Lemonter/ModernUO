using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/ZombieCostume.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ZombieCostume : BaseCostume
{
    public override string CreatureName => "zombie";

    [Constructible]
    public ZombieCostume() : base( )
    {
        this.CostumeBody = 3;
    }

    public override int LabelNumber => 1114222; // zombie costume
}
