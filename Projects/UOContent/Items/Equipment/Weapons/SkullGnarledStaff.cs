using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/SkullGnarledStaff.cs).
/// </summary>
[Flippable(41795, 41796)]
[SerializationGenerator(0, false)]
public partial class SkullGnarledStaff : GnarledStaff
{
    public override int LabelNumber => 1125819; // skull gnarled staff

    [Constructible]
    public SkullGnarledStaff()
    {
        ItemID = 41795;
    }
}

[Flippable(41799, 41800)]
[SerializationGenerator(0, false)]
public partial class GargishSkullGnarledStaff : GnarledStaff
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1125823; // gargish skull gnarled staff

    [Constructible]
    public GargishSkullGnarledStaff()
    {
        ItemID = 41799;
    }
}
