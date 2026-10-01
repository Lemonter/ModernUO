using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The six offerings the Twisted Weald altar accepts, ported from ServUO
/// (Scripts/Items/Quest/). Each is dropped by the Weald creature it's named for; three of
/// them, of any kind, wake Dread Horn.
///
/// Kept in one file rather than six, since they are six identical two-line classes — the
/// originals are separate files only because ServUO keeps one type per file.</summary>
[SerializationGenerator(0, false)]
public partial class BlightedCotton : PeerlessKey
{
    [Constructible]
    public BlightedCotton() : base(0x2DB)
    {
        Weight = 1.0;
        Hue = 0x35;
    }

    public override int LabelNumber => 1074331; // blighted cotton
}

[SerializationGenerator(0, false)]
public partial class GnawsFang : PeerlessKey
{
    [Constructible]
    public GnawsFang() : base(0x10E8)
    {
        Weight = 1.0;
        Hue = 0x174;
    }

    public override int LabelNumber => 1074332; // gnaw's fang
}

[SerializationGenerator(0, false)]
public partial class LissithsSilk : PeerlessKey
{
    [Constructible]
    public LissithsSilk() : base(0x2001)
    {
        Weight = 1.0;
        Hue = 0x4FB;
    }

    public override int LabelNumber => 1074333; // lissith's silk
}

[SerializationGenerator(0, false)]
public partial class ThornyBriar : PeerlessKey
{
    [Constructible]
    public ThornyBriar() : base(Utility.RandomList(0x3020, 0x3021, 0x3022, 0x3023, 0x3024))
    {
        Weight = 1.0;
        Hue = 0x214;
    }

    public override int LabelNumber => 1074334; // thorny briar
}

[SerializationGenerator(0, false)]
public partial class IrksBrain : PeerlessKey
{
    [Constructible]
    public IrksBrain() : base(0x1CF0)
    {
        Weight = 1.0;
        Hue = 0x453;
    }

    public override int LabelNumber => 1074335; // irk's brain
}

[SerializationGenerator(0, false)]
public partial class SabrixsEye : PeerlessKey
{
    [Constructible]
    public SabrixsEye() : base(0xF87)
    {
        Weight = 1.0;
        Hue = 0x480;
    }

    public override int LabelNumber => 1074336; // sabrix's eye
}
