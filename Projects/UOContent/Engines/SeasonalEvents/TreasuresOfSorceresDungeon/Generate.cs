using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Generate.cs).
///     ServUO synced this against the points system on every world save; there is no points system here, so the
///     controller is <b>not auto-started</b>: nothing is placed until a GM runs the command. The seasonal switch is
///     <see cref="SorcerersDungeonData.Enabled" />.
/// </summary>
public static class SorcerersDungeonGeneration
{
    private static readonly Point3D ResearcherLocation = new(536, 456, -53);

    public static void Configure()
    {
        CommandSystem.Register("GenerateTreasuresOfSorcerersDungeon", AccessLevel.GameMaster, Generate_OnCommand);
    }

    private static void Generate_OnCommand(CommandEventArgs e)
    {
        Generate();

        e.Mobile.SendMessage("Контент Treasures of Sorcerer's Dungeon размещён.");
    }

    public static void Generate()
    {
        var map = Map.Ilshenar;

        if (SorcerersDungeonResearcher.Instance == null || SorcerersDungeonResearcher.Instance.Deleted)
        {
            SorcerersDungeonResearcher.Instance = new SorcerersDungeonResearcher();
            SorcerersDungeonResearcher.Instance.MoveToWorld(ResearcherLocation, map);
        }

        if (!AnyNearby<Static>(new Point3D(546, 460, 6), map))
        {
            new Static(0x9D2B).MoveToWorld(new Point3D(546, 460, 6), map);
            new Static(0x9D2C).MoveToWorld(new Point3D(548, 460, 6), map);
            new Static(0x9D2D).MoveToWorld(new Point3D(548, 458, 6), map);
        }

        if (!AnyNearby<Static>(new Point3D(545, 462, -53), map))
        {
            new Static(0x9F34).MoveToWorld(new Point3D(545, 462, -53), map);
        }

        if (!AnyNearby<Static>(new Point3D(550, 462, -53), map))
        {
            new Static(0x9F34).MoveToWorld(new Point3D(550, 462, -53), map);
        }

        if (!AnyNearby<Static>(new Point3D(545, 463, -55), map))
        {
            new Static(0x9F28).MoveToWorld(new Point3D(545, 463, -55), map);
        }

        if (!AnyNearby<Static>(new Point3D(550, 463, -55), map))
        {
            new Static(0x9F24).MoveToWorld(new Point3D(550, 463, -55), map);
        }

        if (TOSDSpawner.Instance == null)
        {
            new TOSDSpawner().BeginTimer();
        }

        SorcerersDungeonData.Enabled = true;
    }

    public static void Remove()
    {
        TOSDSpawner.Instance?.Deactivate();

        SorcerersDungeonData.Enabled = false;
    }

    private static bool AnyNearby<T>(Point3D p, Map map) where T : Item
    {
        foreach (var item in map.GetItemsInRange<T>(p, 0))
        {
            if (!item.Deleted)
            {
                return true;
            }
        }

        return false;
    }
}
