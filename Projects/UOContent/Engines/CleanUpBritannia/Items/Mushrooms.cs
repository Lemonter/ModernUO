using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Mushrooms1 : Item
{
    [Constructible]
    public Mushrooms1() : base(0x0D0F)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "грибы";
}

[SerializationGenerator(0, false)]
public partial class Mushrooms2 : Item
{
    [Constructible]
    public Mushrooms2() : base(0x0D12)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "грибы";
}

[SerializationGenerator(0, false)]
public partial class Mushrooms3 : Item
{
    [Constructible]
    public Mushrooms3() : base(0x0D10)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "грибы";
}

[SerializationGenerator(0, false)]
public partial class Mushrooms4 : Item
{
    [Constructible]
    public Mushrooms4() : base(0x0D13)
    {
        Weight = 1.0;
    }

    public override string DefaultName => "грибы";
}
