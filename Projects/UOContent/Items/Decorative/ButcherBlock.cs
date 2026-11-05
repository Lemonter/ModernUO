using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ButcherBlock : Item
{
    [Constructible]
    public ButcherBlock() : base(0xA2A3) => LootType = LootType.Blessed;

    public override int LabelNumber => 1125659; // butcher block
}
