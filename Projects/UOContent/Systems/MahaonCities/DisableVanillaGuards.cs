using Server.Regions;

namespace Server.Systems.MahaonCities;

/// <summary>Отключает ванильную стражу городов целиком — у нас своя система через
/// захват (CityControlSystem: SpawnGuards/DespawnGuards привязаны к владеющей городом
/// гильдии), ванильная (GuardedRegion.CallGuards/MakeGuard) дублирует и конфликтует с
/// ней. Не удаляет сами GuardedRegion (нужны для другой логики — запрет каста в городе,
/// доступ к вендорам преступникам и т.д.), только выключает конкретно спавн стражи через
/// GuardsDisabled — то же самое поле, что уже используют команды ГМ SetGuarded/
/// ToggleGuarded, просто применяем ко всем регионам разом при старте.</summary>
public static class DisableVanillaGuards
{
    public static void Initialize()
    {
        var count = 0;

        foreach (var region in Region.Regions)
        {
            if (region is GuardedRegion guarded)
            {
                guarded.GuardsDisabled = true;
                count++;
            }
        }

        Server.Logging.LogFactory.GetLogger(typeof(DisableVanillaGuards))
            .Information($"Отключена ванильная стража в {count} регионах.");
    }
}
