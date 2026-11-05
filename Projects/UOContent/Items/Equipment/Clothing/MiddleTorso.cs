using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/MiddleTorso.cs).
/// </summary>
[Flippable(0x46B4, 0x46B5)]
[SerializationGenerator(0, false)]
public partial class GargishSash : BaseClothing
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishSash()
        : this(0)
    {
    }

    public GargishSash(int hue)
        : base(0x46B4, Layer.MiddleTorso, hue)
    {
        this.Weight = 1.0;
    }
}
