using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA497, 0xA498)]
[SerializationGenerator(0, false)]
public partial class PillowSkull : Item
{
    [Constructible]
    public PillowSkull() : base(0xA497)
    {
    }

    public override int LabelNumber => 1025015; // pillow

    public override double DefaultWeight => 1.0;
}
