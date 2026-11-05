using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x9A1C, 0x9A1D)]
[SerializationGenerator(0, false)]
public partial class JadeSkull : Item
{
    [Constructible]
    public JadeSkull() : base(0x9A1C)
    {
    }

    public override int LabelNumber => 1123476; // Jade Skull
}
