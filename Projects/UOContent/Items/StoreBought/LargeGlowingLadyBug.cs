using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class LargeGlowingLadyBug : BaseLight
{
    [Constructible]
    public LargeGlowingLadyBug() : base(0x2CFD)
    {
        Duration = TimeSpan.Zero; // Never burnt out
        Burning = false;
        Light = LightType.Circle225;
        Weight = 3.0;
    }

    public override int LabelNumber => 1071400; // Large Glowing Lady Bug

    public override int LitItemID => SouthFacing ? 0x2CFE : 0x2D00;

    public override int UnlitItemID => SouthFacing ? 0x2CFD : 0x2CFF;

    public bool SouthFacing => ItemID == 0x2CFD || ItemID == 0x2CFE;

    public void Flip()
    {
        ItemID = ItemID switch
        {
            0x2CFD => 0x2CFF,
            0x2CFE => 0x2D00,
            0x2CFF => 0x2CFD,
            0x2D00 => 0x2CFE,
            _      => ItemID
        };
    }
}
