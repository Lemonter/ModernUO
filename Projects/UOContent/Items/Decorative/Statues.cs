using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StatuePegasusSouth : Item
{
    [Constructible]
    public StatuePegasusSouth() : base(0x139D)
    {
    }

    public override int LabelNumber => 1044510; // pegasus statuette

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 1.0;
}

[SerializationGenerator(0, false)]
public partial class StatuePegasusEast : Item
{
    [Constructible]
    public StatuePegasusEast() : base(0x1228)
    {
    }

    public override int LabelNumber => 1044510; // pegasus statuette

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 1.0;
}

[Flippable(0x494E, 0x494D)]
[SerializationGenerator(0, false)]
public partial class StatueGargoyleEast : Item
{
    [Constructible]
    public StatueGargoyleEast() : base(0x494E)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 1.0;
}

[Flippable(0x494D, 0x494E)]
[SerializationGenerator(0, false)]
public partial class StatueGargoyleSouth : Item
{
    [Constructible]
    public StatueGargoyleSouth() : base(0x494D)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 1.0;
}

[Flippable(0x493C, 0x493B)]
[SerializationGenerator(0, false)]
public partial class StatueGryphonEast : Item
{
    [Constructible]
    public StatueGryphonEast() : base(0x493C)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 1.0;
}

[Flippable(0x493B, 0x493C)]
[SerializationGenerator(0, false)]
public partial class StatueGryphonSouth : Item
{
    [Constructible]
    public StatueGryphonSouth() : base(0x493B)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 1.0;
}
