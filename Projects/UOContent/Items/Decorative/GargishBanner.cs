using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x4037, 0x4038)]
[SerializationGenerator(0, false)]
public partial class GargishBanner : Item
{
    [Constructible]
    public GargishBanner() : base(0x4037)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 10.0;
}
