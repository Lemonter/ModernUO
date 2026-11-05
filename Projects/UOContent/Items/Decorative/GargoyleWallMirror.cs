using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x4044, 0x4045)]
[SerializationGenerator(0, false)]
public partial class GargoyleWallMirror : Item
{
    [Constructible]
    public GargoyleWallMirror() : base(0x4044)
    {
    }

    public override double DefaultWeight => 10.0;
}
