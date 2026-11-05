using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA48D, 0xA48E)]
[SerializationGenerator(0, false)]
public partial class PaintingAnkh : Item
{
    [Constructible]
    public PaintingAnkh() : base(0xA48D)
    {
    }

    public override int LabelNumber => 1023744; // painting

    public override double DefaultWeight => 10.0;
}
