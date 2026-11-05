using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TableLamp : Item
{
    [Constructible]
    public TableLamp() : base(0x49C2)
    {
        Weight = 1;
    }

    public override int LabelNumber => 1151220; // table lamp

    public override void OnDoubleClick(Mobile from)
    {
        if (ItemID == 0x49C2)
        {
            ItemID = 0x49C1;
        }
        else if (ItemID == 0x49C1)
        {
            ItemID = 0x49C2;
        }
    }
}
