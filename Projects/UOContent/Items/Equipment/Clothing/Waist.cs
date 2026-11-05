using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/Waist.cs).
/// </summary>
[Flippable(0x50D8, 0x50D9)]
[SerializationGenerator(0, false)]
public partial class GargishApron : BaseWaist
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishApron()
        : this(0)
    {
    }

    public GargishApron(int hue)
        : base(0x50D8, hue)
    {
        this.Weight = 2.0;
    }
}
