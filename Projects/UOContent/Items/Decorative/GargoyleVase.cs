using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x4042, 0x4043)]
[SerializationGenerator(0, false)]
public partial class GargoyleVase : Item
{
    [Constructible]
    public GargoyleVase() : base(0x4042)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 10.0;
}
