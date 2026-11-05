using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x403A, 0x4046)]
[SerializationGenerator(0, false)]
public partial class GargoyleFloorMirror : Item
{
    [Constructible]
    public GargoyleFloorMirror() : base(0x403A)
    {
    }

    public override double DefaultWeight => 10.0;
}
