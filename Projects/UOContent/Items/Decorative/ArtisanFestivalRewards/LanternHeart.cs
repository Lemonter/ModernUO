using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class LanternHeart : BaseLight
{
    [Constructible]
    public LanternHeart() : base(0xA481)
    {
    }

    public override int LabelNumber => 1011221; // lantern

    public override int LitItemID => ItemID == 0xA481 ? 0xA482 : 0xA486;

    public override int UnlitItemID => ItemID == 0xA482 ? 0xA481 : 0xA485;

    public int NorthID => Burning ? 0xA482 : 0xA481;

    public int WestID => Burning ? 0xA486 : 0xA485;

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
