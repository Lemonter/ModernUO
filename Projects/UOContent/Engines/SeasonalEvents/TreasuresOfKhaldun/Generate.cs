using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Generate.cs).
///     Per the season-port rules this controller is <b>not auto-started</b>: <see cref="KhaldunData.Enabled" />
///     defaults to false and nothing is placed until a GM flips it and calls <see cref="Generate" />. The automatic
///     on/off sync against <c>PointsSystem.Khaldun.InSeason</c> is gone with the points system.
///     Skipped: the Khaldun champion spawn (<c>ChampionSpawnType.Khaldun</c> is not registered in this codebase's
///     <c>ChampionSpawnInfo.Table</c>, and <c>ChampionSystem.AllSpawns</c> does not exist) and the Going Gumshoe
///     quest content (<c>GenerateQuestContent</c> needs the quest's item chain, which is not part of this pack).
/// </summary>
public static class TreasuresOfKhaldunGeneration
{
    /// <summary>Автозапуск запрещён: включается вручную.</summary>
    public static bool Enabled
    {
        get => KhaldunData.Enabled;
        set => KhaldunData.Enabled = value;
    }

    public static void Generate()
    {
        PlaceResearcher(Map.Trammel, ref _researcherTram, new Point3D(6009, 3771, 21));
        PlaceResearcher(Map.Felucca, ref _researcherFel, new Point3D(6009, 3771, 21));

        PlaceInvestigator(Map.Trammel, ref _investigatorTram, new Point3D(6010, 3776, 19));
        PlaceInvestigator(Map.Felucca, ref _investigatorFel, new Point3D(6010, 3776, 19));

        PlaceVendor(Map.Trammel, ref _vendorTram, new Point3D(6018, 3749, 21));
        PlaceVendor(Map.Felucca, ref _vendorFel, new Point3D(6018, 3749, 21));

        KhaldunData.Enabled = true;
    }

    public static void Remove()
    {
        _researcherTram?.Delete();
        _researcherFel?.Delete();
        _investigatorTram?.Delete();
        _investigatorFel?.Delete();
        _vendorTram?.Delete();
        _vendorFel?.Delete();

        _researcherTram = null;
        _researcherFel = null;
        _investigatorTram = null;
        _investigatorFel = null;
        _vendorTram = null;
        _vendorFel = null;

        KhaldunData.Enabled = false;
    }

    private static KhaldunResearcher _researcherTram;
    private static KhaldunResearcher _researcherFel;
    private static LeadInvestigator _investigatorTram;
    private static LeadInvestigator _investigatorFel;
    private static CaddelliteVendor _vendorTram;
    private static CaddelliteVendor _vendorFel;

    private static void PlaceResearcher(Map map, ref KhaldunResearcher instance, Point3D location)
    {
        if (instance != null && !instance.Deleted)
        {
            return;
        }

        instance = new KhaldunResearcher();
        instance.MoveToWorld(location, map);
    }

    private static void PlaceInvestigator(Map map, ref LeadInvestigator instance, Point3D location)
    {
        if (instance != null && !instance.Deleted)
        {
            return;
        }

        instance = new LeadInvestigator();
        instance.MoveToWorld(location, map);
    }

    private static void PlaceVendor(Map map, ref CaddelliteVendor instance, Point3D location)
    {
        if (instance != null && !instance.Deleted)
        {
            return;
        }

        instance = new CaddelliteVendor();
        instance.MoveToWorld(location, map);
    }
}
