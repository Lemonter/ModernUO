using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[SerializationGenerator(0, false)]
public partial class Countertop : Item
{
    [Constructible]
    public Countertop() : base(0xA2DA) => LootType = LootType.Blessed;

    public override int LabelNumber => 1125714; // countertop
}
