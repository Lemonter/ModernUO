using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonQuests;

/// <summary>Постоянная стрелка-компас к точке клада — переиспользует тот же сетевой
/// пакет, что и ванильная система квест-стрелок (OutgoingArrowPackets.SendSetArrow),
/// напрямую, без обёртки QuestArrow (та привязана к живому Mobile-цели, берёт X/Y
/// с него — цель клада статична, обёртка не нужна вообще). Серийник в пакете
/// используется только клиентами HighSeas-эры для правого клика по стрелке — передаём
/// серийник самой карты, осмысленно и безопасно.</summary>
public static class MahaonTreasureCompassSystem
{
    private static readonly Dictionary<PlayerMobile, TreasureMap> Active = new();

    public static bool IsTracking(PlayerMobile player) => Active.ContainsKey(player);

    public static void Start(PlayerMobile player, TreasureMap map)
    {
        Stop(player); // сменить цель — сначала гасим старую стрелку, не накладываем поверх

        Active[player] = map;
        player.NetState?.SendSetArrow(map.ChestLocation.X, map.ChestLocation.Y, map.Serial);
        player.SendMessage(0x59, "Компас включён — стрелка укажет направление к кладу.");
    }

    public static void Stop(PlayerMobile player)
    {
        if (!Active.Remove(player, out var map))
        {
            return;
        }

        player.NetState?.SendCancelArrow(map.ChestLocation.X, map.ChestLocation.Y, map.Serial);
    }

    /// <summary>Клад выкопан/карта удалена — гасим стрелку сами, не дожидаясь, пока
    /// игрок додумается выключить вручную.</summary>
    public static void StopIfTracking(TreasureMap map)
    {
        PlayerMobile trackingPlayer = null;

        foreach (var (player, trackedMap) in Active)
        {
            if (trackedMap == map)
            {
                trackingPlayer = player;
                break;
            }
        }

        // Remove происходит уже ПОСЛЕ завершения перебора — не мутируем Active изнутри
        // foreach по нему же.
        if (trackingPlayer != null)
        {
            Stop(trackingPlayer);
        }
    }
}
