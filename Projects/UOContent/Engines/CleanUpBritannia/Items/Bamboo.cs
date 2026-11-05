using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bamboo : Item
{
    [Constructible]
    public Bamboo() : base(0x246D)
    {
        Weight = 10;
    }

    public override int LabelNumber => 1029324; // bamboo
}
