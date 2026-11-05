using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/OuterLegs.cs).
/// </summary>

[SerializationGenerator(0, false)]
public partial class GuildedKilt : BaseOuterLegs
{
    public override int LabelNumber => 1109619; // Guilded Kilt

    [Constructible]
    public GuildedKilt()
        : this(0)
    {
    }

    public GuildedKilt(int hue)
        : base(0x781B, hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CheckeredKilt : BaseOuterLegs
{
    public override int LabelNumber => 1109620; // Checkered Kilt

    [Constructible]
    public CheckeredKilt()
        : this(0)
    {
    }

    public CheckeredKilt(int hue)
        : base(0x781C, hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class FancyKilt : BaseOuterLegs
{
    public override int LabelNumber => 1109621; // Fancy Kilt

    [Constructible]
    public FancyKilt()
        : this(0)
    {
    }

    public FancyKilt(int hue)
        : base(0x781D, hue)
    {
    }
}
