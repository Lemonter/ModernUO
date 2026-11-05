using ModernUO.Serialization;
using System;
using Server.Engines.Craft;
using Server.Engines.VeteranRewards;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/Cloaks.cs).
/// </summary>
[Flippable(0x4002, 0x4003)]
[SerializationGenerator(0, false)]
public partial class GargishFancyRobe : BaseClothing
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyRobe()
        : this(0)
    {
    }

    public GargishFancyRobe(int hue)
        : base(0x4002, Layer.OuterTorso, hue)
    {
        Weight = 3.0;
    }
}

[Flippable(0x4000, 0x4001)]
[SerializationGenerator(0, false)]
public partial class GargishRobe : BaseClothing
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobe()
        : this(0)
    {
    }

    public GargishRobe(int hue)
        : base(0x4000, Layer.OuterTorso, hue)
    {
        Weight = 3.0;
    }
}
