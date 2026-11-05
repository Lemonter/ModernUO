using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ValentinesTable : Item
{
    [Constructible]
    public ValentinesTable() : base(0xA004)
    {
        Weight = 10.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1098492; // table

    public override void OnDoubleClick(Mobile m) => ItemID = ItemID == 0xA004 ? 0xA005 : 0xA004;
}

[Flippable(0xA05C, 0xA05D, 0xA05E, 0xA05F)]
[SerializationGenerator(0, false)]
public partial class ValentinesChair : Item
{
    [Constructible]
    public ValentinesChair() : base(0xA05C)
    {
        Weight = 20.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1098456; // chair
}
