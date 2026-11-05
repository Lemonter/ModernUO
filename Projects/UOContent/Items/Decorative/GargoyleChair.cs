using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x4023, 0x4024)]
[SerializationGenerator(0, false)]
public partial class GargoyleChair : Item
{
    [Constructible]
    public GargoyleChair() : base(0x4023)
    {
    }

    public override double DefaultWeight => 6.0;
}
