using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA491, 0xA492)]
[SerializationGenerator(0, false)]
public partial class PillowChalice : Item
{
    [Constructible]
    public PillowChalice() : base(0xA491)
    {
    }

    public override int LabelNumber => 1025015; // pillow

    public override double DefaultWeight => 1.0;
}
