using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA339, 0xA33A)]
[SerializationGenerator(0, false)]
public partial class DecorativeHourglass : Item
{
    [Constructible]
    public DecorativeHourglass() : base(0xA339)
    {
    }

    public override int LabelNumber => 1125809; // hourglass
}
