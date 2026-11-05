using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x403D, 0x403E)]
[SerializationGenerator(0, false)]
public partial class GargoylePainting : Item
{
    [Constructible]
    public GargoylePainting() : base(0x403D)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 10.0;
}
