using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class TallDoubleLamp : BaseLight
{
    [Constructible]
    public TallDoubleLamp() : base(0x4C56)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(60) : TimeSpan.Zero;
        Burning = false;
        Light = LightType.Circle225;
        Weight = 1.0;
    }

    public override int LitItemID => ItemID == 0x4C56 ? 0x4C57 : 0x4C59;

    public override int UnlitItemID => ItemID == 0x4C57 ? 0x4C56 : 0x4C58;

    public int NorthID => Burning ? 0x4C57 : 0x4C56;

    public int WestID => Burning ? 0x4C59 : 0x4C58;

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
