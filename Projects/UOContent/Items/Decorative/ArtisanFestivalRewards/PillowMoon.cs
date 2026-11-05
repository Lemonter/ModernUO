using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA495, 0xA496)]
[SerializationGenerator(0, false)]
public partial class PillowMoon : Item
{
    [Constructible]
    public PillowMoon() : base(0xA495)
    {
    }

    public override int LabelNumber => 1025015; // pillow

    public override double DefaultWeight => 1.0;
}
