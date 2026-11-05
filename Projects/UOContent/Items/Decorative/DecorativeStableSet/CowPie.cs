using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA489, 0xA48A)]
[SerializationGenerator(0, false)]
public partial class CowPie : Item
{
    [Constructible]
    public CowPie() : base(0xA4E5)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1126237; // cow pie

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, Location, 2) &&
            !Utility.InRange(oldLocation, Location, 2))
        {
            Effects.PlaySound(Location, Map, 1064);
        }

        base.OnMovement(m, oldLocation);
    }
}
