using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class FlamingScarecrow : BaseLight
{
    [Constructible]
    public FlamingScarecrow() : base(0x9F33)
    {
        Duration = TimeSpan.Zero;
        Burning = false;
    }

    public override int LabelNumber => 1027732; // scarecrow

    public override int LitItemID => ItemID == 0x9F33 ? 0x9F34 : 0x9F3A;

    public override int UnlitItemID => ItemID == 0x9F34 ? 0x9F33 : 0x9F39;

    public void Flip()
    {
        if (ItemID is 0x9F33 or 0x9F34)
        {
            ItemID += 6;
        }
        else
        {
            ItemID -= 6;
        }
    }
}
