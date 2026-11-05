using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA493, 0xA494)]
[SerializationGenerator(0, false)]
public partial class PillowTear : Item
{
    [Constructible]
    public PillowTear() : base(0xA493)
    {
    }

    public override int LabelNumber => 1025015; // pillow

    public override double DefaultWeight => 1.0;
}
