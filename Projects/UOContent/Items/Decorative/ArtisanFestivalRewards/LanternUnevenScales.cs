using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class LanternUnevenScales : BaseLight
{
    [Constructible]
    public LanternUnevenScales() : base(0xA479)
    {
    }

    public override int LabelNumber => 1011221; // lantern

    public override int LitItemID => ItemID == 0xA479 ? 0xA47A : 0xA47E;

    public override int UnlitItemID => ItemID == 0xA47A ? 0xA479 : 0xA47D;

    public int NorthID => Burning ? 0xA47A : 0xA479;

    public int WestID => Burning ? 0xA47E : 0xA47D;

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
