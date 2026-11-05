using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[SerializationGenerator(0, false)]
public partial class TallMusicStandLeft : Item
{
    [Constructible]
    public TallMusicStandLeft() : base(0xEBB)
    {
    }

    public override double DefaultWeight => 10.0;
}

[Furniture]
[SerializationGenerator(0, false)]
public partial class TallMusicStandRight : Item
{
    [Constructible]
    public TallMusicStandRight() : base(0xEBC)
    {
    }

    public override double DefaultWeight => 10.0;
}

[Furniture]
[SerializationGenerator(0, false)]
public partial class ShortMusicStandLeft : Item
{
    [Constructible]
    public ShortMusicStandLeft() : base(0xEB6)
    {
    }

    public override double DefaultWeight => 10.0;
}

[Furniture]
[SerializationGenerator(0, false)]
public partial class ShortMusicStandRight : Item
{
    [Constructible]
    public ShortMusicStandRight() : base(0xEB8)
    {
    }

    public override double DefaultWeight => 10.0;
}
