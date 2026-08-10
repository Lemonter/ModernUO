using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Silver weapons deal x3 to undead, gold weapons deal x3 to dragon-kind (see
///     BaseCreature.IsUndead / IsDragonKind). Gold already exists as a real CraftResource
///     for metal weapons, so that side just checks Resource — there's no equivalent
///     "Silver" CraftResource in this engine at all, so silver is detected by the weapon's
///     name instead (a GM can just name/rename any weapon to include "серебр" to opt it
///     in, no new resource/hue system needed).
/// </summary>
public static class MetalDamageSystem
{
    private const int TripleDamageBonus = 200; // added to percentageBonus's 0-baseline = x3 total

    public static int GetBonus(BaseWeapon weapon, Mobile defender)
    {
        if (defender is not BaseCreature creature)
        {
            return 0;
        }

        if (creature.IsUndead && IsSilver(weapon))
        {
            return TripleDamageBonus;
        }

        if (creature.IsDragonKind && weapon.Resource == CraftResource.Gold)
        {
            return TripleDamageBonus;
        }

        return 0;
    }

    private static bool IsSilver(BaseWeapon weapon) =>
        weapon.Name?.Contains("серебр", StringComparison.OrdinalIgnoreCase) == true;
}
