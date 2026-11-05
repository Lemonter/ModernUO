using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class LanternBrokenShield : BaseLight
{
    [Constructible]
    public LanternBrokenShield() : base(0xA469)
    {
    }

    public override int LabelNumber => 1011221; // lantern

    public override int LitItemID => ItemID == 0xA469 ? 0xA46A : 0xA46E;

    public override int UnlitItemID => ItemID == 0xA46A ? 0xA469 : 0xA46D;

    public int NorthID => Burning ? 0xA46A : 0xA469;

    public int WestID => Burning ? 0xA46E : 0xA46D;

    public override double DefaultWeight => 1.0;

    public void Flip()
    {
        if (ItemID == NorthID)
        {
            ItemID = WestID;
        }
        else if (ItemID == WestID)
        {
            ItemID = NorthID;
        }
    }
}
