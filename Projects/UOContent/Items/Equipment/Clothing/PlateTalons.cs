using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/PlateTalons.cs).
/// </summary>
[Flippable(0x42DE, 0x42DF)]
[SerializationGenerator(0, false)]
public partial class PlateTalons : BaseShoes
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public PlateTalons()
        : this(0)
    {
    }

    public PlateTalons(int hue)
        : base(0x42DE, hue)
    {
        this.Weight = 5.0;
    }

    public override CraftResource DefaultResource => CraftResource.Iron;
}
