using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DungeonWallAndVineAddon : BaseAddon
{
    [Constructible]
    public DungeonWallAndVineAddon()
    {
        AddComponent(new MagicVinesComponent(), 1, 0, 0);
        AddComponent(new DungeonWallComponent(), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class DungeonWallComponent : AddonComponent
{
    [Constructible]
    public DungeonWallComponent() : base(0x0242)
    {
    }

    public override bool HandlesOnMovement => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.X > X)
        {
            from.SendLocalizedMessage(1111659); // You try to examine the strange wall but the vines get in your way.
            return;
        }

        Z += -22;

        Timer.DelayCall(
            TimeSpan.FromSeconds(15.0),
            () =>
            {
                if (!Deleted)
                {
                    Z += 22;
                }
            }
        );
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (Parent == null && m is PlayerMobile && X < m.X && Utility.InRange(Location, m.Location, 3) &&
            !Utility.InRange(Location, oldLocation, 3))
        {
            m.SendLocalizedMessage(1111665); // You notice something odd about the vines covering the wall.
        }
    }
}
