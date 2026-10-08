using Server.Items;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     "Кости лича"/"кости демона" — редкий шанс срезать ножом с трупа лича/демона один
///     случайный предмет соответствующего перекрашенного костяного сета (см.
///     Items/Armor/Mahaon/LichBone*.cs, DemonBone*.cs). Вызывается из переопределённого
///     BaseCreature.OnCarve на Lich и на "настоящих" демонах (Daemon/ChaosDaemon/
///     ArcaneDaemon/DemonKnight) — призванные существа туда не попадают, как и обычные
///     Summoned-мобы в базовой логике OnCarve.
///     Отдельно от ванильного OnCarve, а не вместо него — обычная логика (мясо и т.д.,
///     если есть) отрабатывает как раньше, это лишь добавка поверх.
///     Флаг corpse.Carved НЕ выставляется здесь — это отвечает вызывающий OnCarve
///     (выставляется безусловно, даже если бросок кости не выпал), иначе труп лича/демона
///     без другого лута (мясо, шкуры) остаётся "неразделанным" при неудачном броске и его
///     можно спам-резать до победного результата — обходя саму суть шанса.
/// </summary>
public static class MahaonCarvedBoneSystem
{
    private const double DropChance = 0.25;

    public static void TryDropLichBone(Mobile from, Corpse corpse)
    {
        if (Utility.RandomDouble() >= DropChance)
        {
            return;
        }

        Item piece = Utility.Random(5) switch
        {
            0 => new LichBoneChest(),
            1 => new LichBoneArms(),
            2 => new LichBoneGloves(),
            3 => new LichBoneLegs(),
            _ => new LichBoneHelm()
        };

        GivePiece(from, corpse, piece);
    }

    public static void TryDropDemonBone(Mobile from, Corpse corpse)
    {
        if (Utility.RandomDouble() >= DropChance)
        {
            return;
        }

        Item piece = Utility.Random(5) switch
        {
            0 => new DemonBoneChest(),
            1 => new DemonBoneArms(),
            2 => new DemonBoneGloves(),
            3 => new DemonBoneLegs(),
            _ => new DemonBoneHelm()
        };

        GivePiece(from, corpse, piece);
    }

    private static void GivePiece(Mobile from, Corpse corpse, Item piece)
    {
        if (!from.PlaceInBackpack(piece))
        {
            corpse.DropItem(piece);
        }

        from.SendMessage("Вы вырезаете из трупа кость необычного цвета.");
    }
}
