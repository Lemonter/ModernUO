using Server.Items;
using Server.Mobiles;

namespace Server.Regions;

// Ported from real OSI/ServUO content (Scripts/Services/Underworld/ExperimentalRoom/
// Region.cs) — kicks anyone standing in one of the 3 puzzle rooms back to the entrance if
// they don't have an ExperimentalGem in their pack (players) or are a controlled/summoned
// pet (pets aren't welcome in the puzzle at all). Registered programmatically (not through
// regions.json) by UnderworldPuzzleController, same pattern as MazeOfDeathRegion.
public class ExperimentalRoomRegion : Region
{
    private static readonly Rectangle2D Entrance = new(994, 1114, 4, 4);

    private static readonly Rectangle2D[] RoomRecs =
    {
        new(977, 1104, 15, 10), // RoomOne
        new(977, 1092, 15, 9),  // RoomTwo
        new(977, 1074, 15, 10)  // RoomThree
    };

    private static ExperimentalRoomRegion _instance;

    public static void Initialize()
    {
        if (_instance != null)
        {
            return;
        }

        _instance = new ExperimentalRoomRegion();
        _instance.Register();
    }

    public ExperimentalRoomRegion() : base("Experimental Room", Map.TerMur, DefaultPriority, RoomRecs)
    {
    }

    public override void OnLocationChanged(Mobile m, Point3D oldLocation)
    {
        base.OnLocationChanged(m, oldLocation);

        if (m is BaseCreature { Controlled: true } or BaseCreature { Summoned: true })
        {
            foreach (var rec in RoomRecs)
            {
                if (rec.Contains(m.Location))
                {
                    KickToEntrance(m);

                    var master = ((BaseCreature)m).GetMaster();

                    if (master?.NetState != null)
                    {
                        master.SendLocalizedMessage(1113472); // Your pet has been kicked out of the room. This is not a stable!
                    }

                    return;
                }
            }
        }
        else if (m is PlayerMobile { AccessLevel: AccessLevel.Player } pm)
        {
            if (pm.Backpack == null)
            {
                KickToEntrance(pm);
                return;
            }

            if (pm.Backpack.FindItemByType(typeof(ExperimentalGem)) != null)
            {
                return;
            }

            foreach (var rec in RoomRecs)
            {
                if (rec.Contains(pm.Location))
                {
                    KickToEntrance(pm);
                    return;
                }
            }
        }
    }

    private static void KickToEntrance(Mobile from)
    {
        if (from?.Map == null)
        {
            return;
        }

        var x = Utility.RandomMinMax(Entrance.X, Entrance.X + Entrance.Width);
        var y = Utility.RandomMinMax(Entrance.Y, Entrance.Y + Entrance.Height);
        var z = from.Map.GetAverageZ(x, y);

        var oldLoc = from.Location;
        var p = new Point3D(x, y, z);

        if (from is PlayerMobile)
        {
            BaseCreature.TeleportPets(from, p, Map.TerMur);
        }

        from.MoveToWorld(p, Map.TerMur);

        Effects.SendLocationParticles(EffectItem.Create(oldLoc, from.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
        Effects.SendLocationParticles(EffectItem.Create(p, from.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);
    }
}
