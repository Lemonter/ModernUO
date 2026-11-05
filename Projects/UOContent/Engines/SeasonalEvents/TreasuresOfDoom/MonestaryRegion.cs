using Server.Spells;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Region.cs).
/// </summary>
public class MonestaryRegion : BaseRegion
{
    private static MonestaryRegion _instance;

    public static void Initialize()
    {
        if (_instance != null)
        {
            return;
        }

        _instance = new MonestaryRegion();
        _instance.Register();
    }

    public MonestaryRegion() : base("Doom Monestary", Map.Malas, DefaultPriority, new Rectangle2D(64, 204, 99, 37))
    {
        GoLocation = new Point3D(79, 223, -1);
    }

    public override bool CheckTravel(Mobile traveller, Point3D p, TravelCheckType type, out TextDefinition message)
    {
        message = null;

        if (traveller.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        return type is TravelCheckType.TeleportTo or TravelCheckType.TeleportFrom;
    }
}
