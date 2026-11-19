using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Сквозь кого бот проходит, не толкаясь: другие боты и мирные НПС — торговцы, стража, ручные и
/// неагрессивные животные, пока ни с кем не дерутся. В городах их сотни, и каждый шаг, упёршийся в
/// торговца у лавки или в оленя на площади, заставлял прокладывать отрезок пути заново. Монстры и
/// всякий, кто в драке, по-прежнему загораживают дорогу: ими держат проходы.
/// </summary>
public static class BotPassage
{
    public static bool IsPeaceful(Mobile m) =>
        m is BaseCreature { Alive: true, Combatant: null } bc &&
        (bc is BaseVendor or CityGuard || bc.Controlled || bc.IsInvulnerable ||
         bc.FightMode is FightMode.None or FightMode.Aggressor);

    public static bool CanPass(Mobile walker, Mobile other) =>
        walker is MahaonDungeons.DungeonWalker || walker is BotMobile && (other is BotMobile || IsPeaceful(other));
}
