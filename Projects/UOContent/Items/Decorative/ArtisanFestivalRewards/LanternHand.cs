using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class LanternHand : BaseLight
{
    [Constructible]
    public LanternHand() : base(0xA471)
    {
    }

    public override int LabelNumber => 1011221; // lantern

    public override int LitItemID => ItemID == 0xA471 ? 0xA472 : 0xA476;

    public override int UnlitItemID => ItemID == 0xA472 ? 0xA471 : 0xA475;

    public int NorthID => Burning ? 0xA472 : 0xA471;

    public int WestID => Burning ? 0xA476 : 0xA475;

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
