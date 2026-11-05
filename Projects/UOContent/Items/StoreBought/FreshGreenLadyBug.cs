using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class FreshGreenLadyBug : BaseLight
{
    [Constructible]
    public FreshGreenLadyBug() : base(0x2D04)
    {
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle225;
        Weight = 3.0;
    }

    public override int LabelNumber => 1071401; // Fresh Green Lady Bug

    public override int LitItemID => SouthFacing ? 0x2D04 : 0x2D02;

    public override int UnlitItemID => SouthFacing ? 0x2D03 : 0x2D01;

    public bool SouthFacing => ItemID == 0x2D03 || ItemID == 0x2D04;

    public void Flip()
    {
        ItemID = ItemID switch
        {
            0x2D01 => 0x2D03,
            0x2D02 => 0x2D04,
            0x2D03 => 0x2D01,
            0x2D04 => 0x2D02,
            _     => ItemID
        };
    }
}
