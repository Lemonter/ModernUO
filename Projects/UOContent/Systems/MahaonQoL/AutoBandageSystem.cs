using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonQoL;

/// <summary>
///     QoL: if you're hurt, carrying bandages, and not already bandaging, apply one to
///     yourself automatically instead of requiring a manual double-click every time.
/// </summary>
public static class AutoBandageSystem
{
    private const double HealthThreshold = 0.85; // only auto-bandage below this fraction of max HP

    public static void TryAutoBandage(PlayerMobile player)
    {
        if (!player.Alive || player.HitsMax <= 0)
        {
            return;
        }

        if ((double)player.Hits / player.HitsMax >= HealthThreshold)
        {
            return;
        }

        if (BandageContext.GetContext(player) != null)
        {
            return; // already bandaging
        }

        var bandage = player.Backpack?.FindItemByType<Bandage>();
        if (bandage == null)
        {
            return;
        }

        Bandage.BandageTargetRequest(player, bandage, player);
    }
}
