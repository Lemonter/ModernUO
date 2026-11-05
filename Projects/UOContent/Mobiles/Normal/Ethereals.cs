using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Ethereals.cs) — only the ethereal statuette
/// types this repository was missing. ServUO's EtherealMount takes a four-argument constructor with
/// an explicit transparent-art hue; ModernUO's takes (itemID, mountID) and derives the hue from
/// EtherealHue, so these newer mounts override EtherealHue to 0 (solid art), which is what the
/// ServUO versions express with Transparent = false.</summary>
[SerializationGenerator(0, false)]
public partial class EtherealBoura : EtherealMount
{
    [Constructible]
    public EtherealBoura() : base(0x46F8, 0x3EC6)
    {
    }

    public override int LabelNumber => 1150006; // Rideable Boura Statuette

    public override int EtherealHue => 0;
}

[SerializationGenerator(0, false)]
public partial class EtherealAncientHellHound : EtherealMount
{
    [Constructible]
    public EtherealAncientHellHound() : base(0x3FFD, 0x3EC9)
    {
    }

    public override int LabelNumber => 1155723; // Ancient Hell Hound Statuette

    public override int EtherealHue => 0;
}

[SerializationGenerator(0, false)]
public partial class EtherealTiger : EtherealMount
{
    [Constructible]
    public EtherealTiger() : this(false)
    {
    }

    public EtherealTiger(bool light = false) : base(0x9844, light ? 0x3EC7 : 0x3EC8)
    {
    }

    public override int EtherealHue => 0;
}

[SerializationGenerator(0, false)]
public partial class EtherealTarantula : EtherealMount
{
    [Constructible]
    public EtherealTarantula() : base(0x9DD6, 0x3ECA)
    {
    }

    public override int LabelNumber => 1157081; // Tarantula Statuette

    public override int EtherealHue => 0;
}

[SerializationGenerator(0, false)]
public partial class EtherealLasher : EtherealMount
{
    [Constructible]
    public EtherealLasher() : base(0x9E35, 0x3ECB)
    {
    }

    public override int LabelNumber => 1157214; // Lasher

    public override int EtherealHue => 0;
}

[SerializationGenerator(0, false)]
public partial class EtherealSerpentineDragon : EtherealMount
{
    [Constructible]
    public EtherealSerpentineDragon() : base(0xA010, 0x3ECE)
    {
    }

    public override int LabelNumber => 1157995; // Ethereal Dragon Statuette

    public override int EtherealHue => 0;
}

[SerializationGenerator(0, false)]
public partial class EtherealWarBoar : EtherealMount
{
    [Constructible]
    public EtherealWarBoar() : base(0xA554, 0x3ED2)
    {
    }

    public override int LabelNumber => 1159423; // Ethereal War Boar Statuette

    public override int EtherealHue => 0;
}
