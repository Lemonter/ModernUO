using Server.Commands;
using Server.Engines.HighSeas;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Generation.cs).
///     ServUO synced this against the points system on every world save; there is no points system here, so the
///     controller is <b>not auto-started</b>: nothing is placed until a GM runs the command. Since the High Seas
///     plunder beacon exists (stage 4), generating the season also starts <see cref="PlunderBeaconSpawner" />, which
///     keeps a set number of beacon platforms in the sea zones; <see cref="Remove" /> takes them down again.
/// </summary>
public static class RisingTideGeneration
{
    private static BlackMarketMerchant _tram;
    private static BlackMarketMerchant _fel;

    public static void Configure()
    {
        CommandSystem.Register("GenerateRisingTide", AccessLevel.GameMaster, Generate_OnCommand);
    }

    private static void Generate_OnCommand(CommandEventArgs e)
    {
        Generate();

        e.Mobile.SendMessage("Контент Rising Tide размещён.");
    }

    public static void Generate()
    {
        if (_tram == null || _tram.Deleted)
        {
            _tram = new BlackMarketMerchant();
            _tram.MoveToWorld(BlackMarketMerchant.SpawnLocation, Map.Trammel);

            _tram.Home = BlackMarketMerchant.SpawnLocation;
            _tram.RangeHome = 2;
        }

        if (_fel == null || _fel.Deleted)
        {
            _fel = new BlackMarketMerchant();
            _fel.MoveToWorld(BlackMarketMerchant.SpawnLocation, Map.Felucca);

            _fel.Home = BlackMarketMerchant.SpawnLocation;
            _fel.RangeHome = 2;
        }

        RisingTideData.Enabled = true;

        // Пиратские маяки (High Seas, этап 4) появляются только вместе с сезоном.
        PlunderBeaconSpawner.Activate();
    }

    public static void Remove()
    {
        _tram?.Delete();
        _fel?.Delete();

        _tram = null;
        _fel = null;

        RisingTideData.Enabled = false;

        PlunderBeaconSpawner.Deactivate();
    }
}
