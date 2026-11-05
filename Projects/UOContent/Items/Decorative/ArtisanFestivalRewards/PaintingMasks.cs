using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA48F, 0xA490)]
[SerializationGenerator(0, false)]
public partial class PaintingMasks : Item
{
    [Constructible]
    public PaintingMasks() : base(0xA48F)
    {
    }

    public override int LabelNumber => 1023744; // painting

    public override double DefaultWeight => 10.0;
}
