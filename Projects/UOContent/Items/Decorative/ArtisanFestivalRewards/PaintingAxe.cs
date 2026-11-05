using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA489, 0xA48A)]
[SerializationGenerator(0, false)]
public partial class PaintingAxe : Item
{
    [Constructible]
    public PaintingAxe() : base(0xA489)
    {
    }

    public override int LabelNumber => 1023744; // painting

    public override double DefaultWeight => 10.0;
}
