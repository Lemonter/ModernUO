using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;

namespace Server.Systems.MahaonCombat;

public enum HitLocation
{
    Chest,
    Arms,
    Legs,
    Hands,
    Neck // 115 Tactics required; flat x2 damage regardless of armor
}

/// <summary>
///     Mahaon "aimed hits": choose a body slot before your next swing. Non-neck locations
///     deal bonus damage scaled to how little physical resistance the defender's armor
///     piece in that slot provides (an empty slot counts as zero resistance — maximum
///     bonus). Neck is a flat x2 once Tactics is high enough, independent of armor,
///     matching the original design exactly.
/// </summary>
public static class HitLocationSystem
{
    private const double NeckTacticsRequirement = 115.0;
    private const double GeneralLocationTacticsRequirement = 50.0;

    private static readonly Dictionary<HitLocation, Layer> LocationLayers = new()
    {
        [HitLocation.Chest] = Layer.InnerTorso,
        [HitLocation.Arms] = Layer.Arms,
        [HitLocation.Legs] = Layer.Pants,
        [HitLocation.Hands] = Layer.Gloves,
        [HitLocation.Neck] = Layer.Neck
    };

    // One pending call per attacker, consumed on the next successful hit.
    private static readonly Dictionary<Mobile, HitLocation> Pending = new();

    // Set at the end of ConsumeHitBonus, right before Pending gets cleared for this
    // attacker — lets BaseWeapon.OnHit ask "what location did that hit that already
    // resolved actually land on" for CombatLogSystem's message, without needing to thread
    // the location through every call in between.
    private static readonly Dictionary<Mobile, HitLocation?> LastConsumed = new();

    public static HitLocation? GetLastConsumedLocation(Mobile attacker) =>
        LastConsumed.TryGetValue(attacker, out var loc) ? loc : null;

    // A hand hit disrupts the defender's spellcasting for a while — tracked separately
    // from the attacker's pending call above, since this is about the DEFENDER.
    private static readonly Dictionary<Mobile, DateTime> HandsBlockUntil = new();
    private static readonly TimeSpan HandsBlockDuration = TimeSpan.FromSeconds(5);

    public static bool IsCastingBlocked(Mobile m) =>
        HandsBlockUntil.TryGetValue(m, out var until) && Core.Now < until;

    public static void Configure()
    {
    }

    public static bool CanUseLocation(Mobile attacker, HitLocation location)
    {
        var tactics = attacker.Skills[SkillName.Tactics].Value;

        return location == HitLocation.Neck
            ? tactics >= NeckTacticsRequirement
            : tactics >= GeneralLocationTacticsRequirement;
    }

    public static void SetPending(Mobile attacker, HitLocation location) => Pending[attacker] = location;

    /// <summary>
    ///     Consumes the attacker's pending called shot (if any) and returns the percentage
    ///     damage bonus/penalty it grants (matching the percentageBonus scale already used
    ///     in BaseWeapon.OnHit — can go negative if the chosen slot turns out to be a bad
    ///     match for the weapon in hand).
    /// </summary>
    public static int ConsumeHitBonus(Mobile attacker, Mobile defender)
    {
        if (!Pending.TryGetValue(attacker, out var location))
        {
            LastConsumed[attacker] = null; // no called shot this swing — a plain hit, not aimed
            return 0;
        }

        Pending.Remove(attacker);

        if (!CanUseLocation(attacker, location))
        {
            LastConsumed[attacker] = null;
            return 0; // skill dropped below requirement since it was called — no bonus
        }

        LastConsumed[attacker] = location;

        if (location == HitLocation.Hands)
        {
            HandsBlockUntil[defender] = Core.Now + HandsBlockDuration;
        }

        BoneFractureSystem.RollFracture(defender, location);

        if (location == HitLocation.Neck)
        {
            return 100; // flat x2, independent of armor — matches the original design
        }

        if (attacker.Weapon is not BaseWeapon weapon)
        {
            return 0;
        }

        var layer = LocationLayers[location];
        var piece = defender.FindItemOnLayer(layer);
        var armorProfile = piece is BaseArmor armor ? DamageTypeSystem.GetArmorProfile(armor) : DamageTypeSystem.NoArmor;
        var weaponProfile = DamageTypeSystem.GetWeaponProfile(weapon);

        var overlap = DamageTypeSystem.GetOverlap(weaponProfile, armorProfile);

        // Baseline is the overlap against a generic 1/3-1/3-1/3 spread — found a real gap
        // (overlap below that) and you get bonus damage; picked a slot that happens to
        // resist your weapon's type well and you take a penalty instead.
        const double baseline = 1.0 / 3.0;
        return (int)((baseline - overlap) * 150);
    }

    /// <summary>
    ///     Which of the currently-usable hit locations is the weakest match for this
    ///     attacker's weapon against this specific defender's gear right now. Used by bots
    ///     (and available to anything else) instead of picking a called shot at random —
    ///     the whole point of aiming is finding an actual gap.
    /// </summary>
    public static HitLocation? GetBestLocation(Mobile attacker, Mobile defender)
    {
        if (attacker.Weapon is not BaseWeapon weapon)
        {
            return null;
        }

        var weaponProfile = DamageTypeSystem.GetWeaponProfile(weapon);

        HitLocation? best = null;
        var bestOverlap = double.MaxValue;

        foreach (var (location, layer) in LocationLayers)
        {
            if (location == HitLocation.Neck || !CanUseLocation(attacker, location))
            {
                continue;
            }

            var piece = defender.FindItemOnLayer(layer);
            var armorProfile = piece is BaseArmor armor ? DamageTypeSystem.GetArmorProfile(armor) : DamageTypeSystem.NoArmor;
            var overlap = DamageTypeSystem.GetOverlap(weaponProfile, armorProfile);

            if (overlap < bestOverlap)
            {
                bestOverlap = overlap;
                best = location;
            }
        }

        // Neck is the exception — it's a flat x2 regardless of armor, so it's always worth
        // it once it's unlocked, no need to compare it against anything.
        if (CanUseLocation(attacker, HitLocation.Neck))
        {
            best = HitLocation.Neck;
        }

        return best;
    }
}
