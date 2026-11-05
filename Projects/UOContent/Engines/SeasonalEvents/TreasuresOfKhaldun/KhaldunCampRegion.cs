using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/KhaldunCampRegion.cs).
///     The region keeps ServUO's guard/blocker population on register and clears it on unregister; because this
///     codebase's <c>Region</c> has no <c>GetEnumeratedMobiles</c>/<c>GetEnumeratedItems</c>, the cleanup uses the
///     map's bounds enumerators instead. Travel of every kind is refused inside the camp. Guards,
///     <see cref="LeadInvestigator" /> and <see cref="KhaldunCampBlocker" /> live in their own files.
/// </summary>
public class KhaldunCampRegion : BaseRegion
{
    private static readonly Point3D[] _guardLocs =
    {
        new(6024, 3713, 1),
        new(6035, 3752, 11),
        new(5999, 3726, 22),
        new(5992, 3752, 9),
        new(6023, 3777, 20)
    };

    private static readonly Point3D[][] _blockerLocs =
    {
        new Point3D[]
        {
            new(5995, 3727, 19), new(5995, 3726, 18), new(5996, 3726, 20), new(5996, 3725, 17), new(5997, 3725, 19),
            new(5997, 3724, 19), new(5998, 3724, 19), new(5998, 3723, 20), new(5999, 3723, 20), new(5999, 3722, 20)
        },
        new Point3D[]
        {
            new(6023, 3710, 0), new(6024, 3710, 1), new(6025, 3710, 2), new(6026, 3710, 1), new(6027, 3710, -2),
            new(6028, 3710, -2), new(6029, 3710, -1), new(6030, 3710, -1), new(6031, 3710, 0)
        },
        new Point3D[]
        {
            new(6036, 3748, 2), new(6037, 3748, 1), new(6038, 3748, 0), new(6039, 3748, 0), new(6040, 3748, 1),
            new(6041, 3748, -2)
        },
        new Point3D[]
        {
            new(6025, 3782, 22), new(6026, 3782, 22), new(6026, 3781, 22), new(6027, 3781, 19), new(6027, 3780, 22),
            new(6027, 3779, 18)
        },
        new Point3D[]
        {
            new(5991, 3755, 4), new(5991, 3754, 9), new(5991, 3753, 8), new(5991, 3752, 8), new(5991, 3751, 8),
            new(5991, 3750, 7), new(5991, 3749, 5)
        },
        new Point3D[]
        {
            new(5992, 3749, 9), new(5993, 3749, 10), new(5994, 3749, 10), new(5995, 3749, 10)
        }
    };

    private static KhaldunCampRegion _instanceTram;
    private static KhaldunCampRegion _instanceFel;

    public KhaldunCampRegion(Map map) : base("Khaldun Camp Region", map, DefaultPriority, new Rectangle2D(5990, 3706, 52, 88))
    {
    }

    public static void Initialize()
    {
        if (_instanceTram == null)
        {
            _instanceTram = new KhaldunCampRegion(Map.Trammel);
            _instanceTram.Register();
        }

        if (_instanceFel == null)
        {
            _instanceFel = new KhaldunCampRegion(Map.Felucca);
            _instanceFel.Register();
        }
    }

    public override void OnRegister()
    {
        foreach (var p in _guardLocs)
        {
            var occupied = false;

            foreach (var guard in Map.GetMobilesInRange<KhaldunCampGuard>(p, 0))
            {
                if (!guard.Deleted)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
            {
                new KhaldunCampGuard().MoveToWorld(p, Map);
            }
        }

        for (var i = 0; i < _blockerLocs.Length; i++)
        {
            foreach (var p in _blockerLocs[i])
            {
                var occupied = false;

                foreach (var blocker in Map.GetItemsInRange<KhaldunCampBlocker>(p, 0))
                {
                    if (!blocker.Deleted)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    new KhaldunCampBlocker(i).MoveToWorld(p, Map);
                }
            }
        }
    }

    public override void OnUnregister()
    {
        foreach (var p in _guardLocs)
        {
            foreach (var guard in Map.GetMobilesInRange<KhaldunCampGuard>(p, 0))
            {
                guard.Delete();
            }
        }

        for (var i = 0; i < _blockerLocs.Length; i++)
        {
            foreach (var p in _blockerLocs[i])
            {
                foreach (var blocker in Map.GetItemsInRange<KhaldunCampBlocker>(p, 0))
                {
                    blocker.Delete();
                }
            }
        }
    }

    public override bool CheckTravel(Mobile traveller, Point3D p, TravelCheckType type, out TextDefinition message)
    {
        message = null;

        return traveller.AccessLevel > AccessLevel.Player;
    }
}
