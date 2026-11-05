using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x1541, 0x1542)]
[SerializationGenerator(0, false)]
public partial class SashOfMight : BodySash
{
    public override int LabelNumber => 1075412; // Sash of Might

    [Constructible]
    public SashOfMight() : base(0x1541)
    {
        Hue = 0x481;
    }
}
