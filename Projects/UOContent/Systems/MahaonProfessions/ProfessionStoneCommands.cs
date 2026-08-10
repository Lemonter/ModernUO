using Server.Commands;
using Server.Items;
using Server.Systems.MahaonCities;

namespace Server.Systems.MahaonProfessions;

public static class ProfessionStoneCommands
{
    public static void Configure()
    {
        CommandSystem.Register("SeedProfessionStone", AccessLevel.GameMaster, SeedProfessionStone_OnCommand);
    }

    [Usage("SeedProfessionStone")]
    [Description("Places the profession stone at Britain's bank (or your current location if no bank marker is set).")]
    private static void SeedProfessionStone_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        Point3D loc;
        Map map;

        if (CityMarkers.TryGetMarker("Britain", "banker", out var markedLoc, out var markedMap))
        {
            loc = markedLoc;
            map = markedMap;
        }
        else
        {
            loc = from.Location;
            map = from.Map;
        }

        var stone = new ProfessionStone();
        stone.MoveToWorld(loc, map);

        from.SendMessage(0x59, "Камень профессий расставлен.");
    }
}
