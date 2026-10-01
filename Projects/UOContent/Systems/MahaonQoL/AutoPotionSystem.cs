using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonQoL;

/// <summary>
///     QoL: auto-drinks the strongest available heal/cure/refresh potion in the backpack
///     when needed, backing up AutoBandageSystem — a bandage started reactively is easily
///     slipped by the very next big hit (see PlayerMobile.OnDamage's disruptThreshold
///     check, which Slip()s any bandage in progress), so a potion (instant, can't be
///     interrupted) is the more reliable safety net while still under fire.
/// </summary>
public static class AutoPotionSystem
{
    private const double HealThreshold = 0.70; // per the shard owner's ask

    // Mahaon: no explicit threshold given for stamina — tuned to feel proactive like the
    // heal threshold, adjust if it fires too eagerly/rarely once tested in-game.
    private const double StaminaThreshold = 0.50;

    public static void TryAutoPotion(PlayerMobile player)
    {
        if (!player.Alive)
        {
            return;
        }

        var pack = player.Backpack;
        if (pack == null)
        {
            return;
        }

        if (player.HitsMax > 0 && (double)player.Hits / player.HitsMax < HealThreshold)
        {
            TryDrink(player, pack.FindItemByType<GreaterHealPotion>(), pack.FindItemByType<HealPotion>(), pack.FindItemByType<LesserHealPotion>());
        }

        if (player.Poisoned)
        {
            TryDrink(player, pack.FindItemByType<GreaterCurePotion>(), pack.FindItemByType<CurePotion>(), pack.FindItemByType<LesserCurePotion>());
        }

        if (player.StamMax > 0 && (double)player.Stam / player.StamMax < StaminaThreshold)
        {
            TryDrink(player, pack.FindItemByType<TotalRefreshPotion>(), pack.FindItemByType<RefreshPotion>());
        }
    }

    private static void TryDrink(Mobile player, params BasePotion[] candidates)
    {
        foreach (var potion in candidates)
        {
            if (potion != null && potion.CanDrink(player))
            {
                potion.Drink(player);
                return;
            }
        }
    }
}
