using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/SkullLongsword.cs).
/// </summary>
[Flippable(41793, 41794)]
[SerializationGenerator(0, false)]
public partial class SkullLongsword : Longsword
{
    public override int LabelNumber => 1125817; // skull longsword

    [Constructible]
    public SkullLongsword()
    {
        ItemID = 41793;
    }
}

[Flippable(41797, 41798)]
[SerializationGenerator(0, false)]
public partial class GargishSkullLongsword : Longsword
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1125821; // gargish skull longsword

    [Constructible]
    public GargishSkullLongsword()
    {
        ItemID = 41797;
    }
}
