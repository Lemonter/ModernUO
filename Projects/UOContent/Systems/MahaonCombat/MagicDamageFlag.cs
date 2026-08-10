namespace Server.Systems.MahaonCombat;

/// <summary>
///     Single-threaded game loop, so a plain static flag is enough — set true right around
///     the one call site in SpellHelper.Damage that actually applies spell damage, false
///     everywhere else (melee, poison ticks, etc). Used so perks like Warrior Fortitude can
///     tell "hit by a sword" from "hit by a fireball" without threading a damage-type
///     parameter through the entire combat pipeline.
/// </summary>
public static class MagicDamageFlag
{
    public static bool Active;
}
