using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA48B, 0xA48C)]
[SerializationGenerator(0, false)]
public partial class PaintingSword : Item
{
    [Constructible]
    public PaintingSword() : base(0xA48B)
    {
    }

    public override int LabelNumber => 1023744; // painting

    public override double DefaultWeight => 10.0;
}
