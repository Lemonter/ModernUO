using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSoulStones;

/// <summary>
///     Drop rule for soul stones. The original correlation between monster and stone
///     type/size was reportedly a mystery even to Mahaon's players, so there's nothing
///     authentic to reconstruct here — this is our own invented rule: flat chance per
///     kill (gated on the soul catcher tattoo), stone SIZE tied to the kill's Fame (the
///     standard vanilla proxy for how tough/notable a monster is — an Orc is 1500, a
///     Dragon/Daemon is 15000), color still uniformly random. Per the shard owner's own
///     examples: "с орка малый, с дракона и демона огромный".
///     Tune DropChance and the Fame breakpoints once real gameplay data exists.
/// </summary>
public static class SoulStoneDropSystem
{
    private const double DropChance = 0.05; // 1 in 20 eligible kills

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        var killer = bc.LastKiller is BaseCreature masterCreature
            ? masterCreature.GetDamageMaster(bc)
            : bc.LastKiller;

        if (killer is not PlayerMobile player)
        {
            return;
        }

        if (!TattooSystem.HasActiveTattoo(player, TattooType.SoulCatcher))
        {
            return;
        }

        if (Utility.RandomDouble() >= DropChance)
        {
            return;
        }

        var size = GetSizeForFame(bc.Fame);

        var color = Utility.RandomList(
            SoulStoneColor.Black,
            SoulStoneColor.Red,
            SoulStoneColor.Blue,
            SoulStoneColor.Green,
            SoulStoneColor.Gold
        );

        var stone = new MahaonSoulStone(size, color);

        if (player.Backpack?.TryDropItem(player, stone, false) != true)
        {
            stone.MoveToWorld(bc.Location, bc.Map);
        }

        player.SendMessage(0x59, $"Выпадает камень души: {SizeRu(size)} {ColorRu(color)}.");
    }

    // Breakpoints calibrated against real Fame values: Orc = 1500 (Small), Dragon/Daemon =
    // 15000 (Giant) — Medium/Large fill the gap for everything in between.
    private static SoulStoneSize GetSizeForFame(int fame) => fame switch
    {
        < 3000  => SoulStoneSize.Small,
        < 8000  => SoulStoneSize.Medium,
        < 15000 => SoulStoneSize.Large,
        _       => SoulStoneSize.Giant
    };

    private static string SizeRu(SoulStoneSize size) => size switch
    {
        SoulStoneSize.Small  => "малый",
        SoulStoneSize.Medium => "средний",
        SoulStoneSize.Large  => "большой",
        SoulStoneSize.Giant  => "гигантский",
        _                    => size.ToString()
    };

    private static string ColorRu(SoulStoneColor color) => color switch
    {
        SoulStoneColor.Black => "чёрный",
        SoulStoneColor.Red   => "красный",
        SoulStoneColor.Blue  => "синий",
        SoulStoneColor.Green => "зелёный",
        SoulStoneColor.Gold  => "золотой",
        _                    => color.ToString()
    };
}
