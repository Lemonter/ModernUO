using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[SerializationGenerator(0, false)]
public partial class EasleSouth : Item
{
    [Constructible]
    public EasleSouth() : base(0xF66)
    {
    }

    public override double DefaultWeight => 25.0;
}

[Furniture]
[SerializationGenerator(0, false)]
public partial class EasleEast : Item
{
    [Constructible]
    public EasleEast() : base(0xF68)
    {
    }

    public override double DefaultWeight => 25.0;
}

[Furniture]
[SerializationGenerator(0, false)]
public partial class EasleNorth : Item
{
    [Constructible]
    public EasleNorth() : base(0xF6A)
    {
    }

    public override double DefaultWeight => 25.0;
}
