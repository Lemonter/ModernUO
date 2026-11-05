using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA4ED, 0xA4EE)]
[SerializationGenerator(0, false)]
public partial class DecorativeStocks : Item
{
    [Constructible]
    public DecorativeStocks() : base(0xA4ED)
    {
        Weight = 1;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159472; // decorative stocks
}
