using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x2DD3, 0x2DD4)]
[SerializationGenerator(0, false)]
public partial class ElvenAlchemyTable : Item
{
    [Constructible]
    public ElvenAlchemyTable() : base(0x2DD3)
    {
    }

    public override int LabelNumber => 1032407; // elven alchemy table

    public override double DefaultWeight => 15.0;
}
