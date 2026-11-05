using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class DragonLamp : BaseLight
{
    [Constructible]
    public DragonLamp() : base(0x4C4C)
    {
        Duration = Burnout ? System.TimeSpan.FromMinutes(60) : System.TimeSpan.Zero;
        Burning = false;
        Light = LightType.Circle225;
        Weight = 1.0;
    }

    public override int LitItemID => ItemID == 0x4C4C ? 0x4C4D : 0x4C4F;

    public override int UnlitItemID => ItemID == 0x4C4D ? 0x4C4C : 0x4C4E;

    public int NorthID => Burning ? 0x4C4D : 0x4C4C;

    public int WestID => Burning ? 0x4C4F : 0x4C4E;

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
