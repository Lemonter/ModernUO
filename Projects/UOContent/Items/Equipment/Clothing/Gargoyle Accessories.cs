using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/Gargoyle Accessories.cs).
/// </summary>
[Flippable(0x450D, 0x450D)]
[SerializationGenerator(0, false)]
public partial class GargoyleTailMale : BaseWaist
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargoyleTailMale()
        : this(0)
    {
    }

    public GargoyleTailMale(int hue)
        : base(0x450D, hue)
    {
        this.Weight = 2.0;
    }
}

[Flippable(0x44C1, 0x44C2)]
[SerializationGenerator(0, false)]
public partial class GargoyleTailFemale : BaseWaist
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargoyleTailFemale()
        : this(0)
    {
    }

    public GargoyleTailFemale(int hue)
        : base(0x44C1, hue)
    {
        this.Weight = 2.0;
    }
}
