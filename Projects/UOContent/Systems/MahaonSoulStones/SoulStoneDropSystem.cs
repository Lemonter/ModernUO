using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonSoulStones;

/// <summary>
///     Drop rule for soul stones. The original correlation between monster and stone
///     type/size was reportedly a mystery even to Mahaon's players, so there's nothing
///     authentic to reconstruct here — this is our own invented rule: flat chance per
///     kill, uniformly random size/color, gated on the soul catcher tattoo.
///     Tune DropChance and the size/color weighting once real gameplay data exists.
/// </summary>
public static class SoulStoneDropSystem
{
    private const double DropChance = 0.005; // 1 in 200 eligible kills

    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
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

        var size = Utility.RandomList(
            SoulStoneSize.Small,
            SoulStoneSize.Medium,
            SoulStoneSize.Large,
            SoulStoneSize.Giant
        );

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
