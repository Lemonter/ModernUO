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
    Back, // 70 Tactics required, like other non-Neck locations but slightly trickier to land
    Neck, // 115 Tactics required; flat x2 damage regardless of armor
    Head  // 50 Tactics, same tier as Chest/Arms/Legs/Hands — no favored style, no armor-profile
          // bonus of its own; exists so a physical attacker can target it to interrupt an
          // enemy casting via "mind" (see CastingChannelSystem).
}

/// <summary>
///     Mahaon "aimed hits": choose a body slot as a standing stance (persists until changed,
///     survives a restart) instead of a one-shot call before a single swing. Non-neck
///     locations deal bonus damage scaled to how little physical resistance the defender's
///     armor piece in that slot provides (an empty slot counts as zero resistance — maximum
///     bonus). Neck is a flat x2 once Tactics is high enough, independent of armor, matching
///     the original design exactly.
///
///     This location choice now ALSO drives which weapon style is "active" — see
///     WeaponStyleSystem.GetActiveStyle, which reads the chosen location here instead of a
///     separately-picked style. Per the shard owner: "стиль теперь идёт от места удара" —
///     direct style selection was removed entirely (IncomingMahaonPackets.cs, the
///     0xF9-select-style handler, is gone).
/// </summary>
public static class HitLocationSystem
{
    private const double NeckTacticsRequirement = 115.0;
    private const double BackTacticsRequirement = 70.0;
    private const double GeneralLocationTacticsRequirement = 50.0;

    // Mahaon: was a strict 1 location -> 1 layer map — a Back hit only ever checked
    // Layer.Cloak, which is almost always 0 armor rating, so body armor never counted
    // toward protecting the back even though it physically covers the torso (back included).
    // Now each location lists every layer that could plausibly be covering it; the actual
    // piece used is whichever candidate gives the best armor rating (see GetBestArmorPiece).
    private static readonly Dictionary<HitLocation, Layer[]> LocationLayers = new()
    {
        [HitLocation.Chest] = new[] { Layer.InnerTorso, Layer.MiddleTorso, Layer.OuterTorso },
        [HitLocation.Arms] = new[] { Layer.Arms },
        [HitLocation.Legs] = new[] { Layer.Pants, Layer.InnerLegs, Layer.OuterLegs },
        [HitLocation.Hands] = new[] { Layer.Gloves },
        [HitLocation.Back] = new[] { Layer.Cloak, Layer.InnerTorso, Layer.MiddleTorso, Layer.OuterTorso },
        [HitLocation.Neck] = new[] { Layer.Neck },
        [HitLocation.Head] = new[] { Layer.Helm }
    };

    // Picks whichever candidate layer for this location actually gives the best protection
    // right now — a BaseArmor piece with the highest ArmorRating wins; if none of the
    // candidates are armor (e.g. just a cloak with no rating), falls back to the first
    // occupied layer so there's still something to name in the combat log.
    private static Item GetBestArmorPiece(Mobile defender, HitLocation location)
    {
        if (!LocationLayers.TryGetValue(location, out var layers))
        {
            return null;
        }

        Item bestArmor = null;
        var bestRating = -1.0;
        Item fallback = null;

        foreach (var layer in layers)
        {
            var piece = defender.FindItemOnLayer(layer);
            if (piece == null)
            {
                continue;
            }

            fallback ??= piece;

            if (piece is BaseArmor armor && armor.ArmorRating > bestRating)
            {
                bestRating = armor.ArmorRating;
                bestArmor = piece;
            }
        }

        return bestArmor ?? fallback;
    }

    // Standing choice per attacker — set via the combat menu, kept until changed, survives
    // a restart (see Persistence below). Used to be one-shot ("Pending", consumed after the
    // very next hit) — the shard owner asked for it to be remembered instead, since style
    // (WeaponStyleSystem) now derives continuously from whatever is chosen here, not just
    // the next swing.
    private static readonly Dictionary<Mobile, HitLocation> Chosen = new();

    // Set at the end of ConsumeHitBonus — lets BaseWeapon.OnHit ask "what location did that
    // hit that already resolved actually land on" for CombatLogSystem's message, without
    // needing to thread the location through every call in between.
    private static readonly Dictionary<Mobile, HitLocation?> LastConsumed = new();

    public static HitLocation? GetLastConsumedLocation(Mobile attacker) =>
        LastConsumed.TryGetValue(attacker, out var loc) ? loc : null;

    public static HitLocation? GetChosenLocation(Mobile attacker) =>
        Chosen.TryGetValue(attacker, out var loc) ? loc : null;

    // A plain (non-called-shot) hit had no location at all in the combat log — shard owner
    // wants every hit to say where it landed, not just aimed ones. Purely cosmetic for a
    // plain hit (no damage bonus/fracture roll — those stay exclusive to a real called
    // shot via ConsumeHitBonus above); excludes Neck since that location is mechanically
    // special (flat x2, Tactics-gated) and randomly implying a neck hit on an ordinary
    // swing would be misleading.
    public static HitLocation RandomDisplayLocation() =>
        (HitLocation)Utility.Random(5); // Chest..Back — Neck/Head deliberately excluded

    // Which armor piece (if any) actually sits at this location right now — used to name
    // it in the combat log ("ударил вас в грудь (по кольчуге) на 12").
    public static Item GetArmorAt(Mobile defender, HitLocation location) => GetBestArmorPiece(defender, location);

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    private static Persistence _persistence;

    public static bool CanUseLocation(Mobile attacker, HitLocation location)
    {
        var tactics = attacker.Skills[SkillName.Tactics].Value;

        return location switch
        {
            HitLocation.Neck => tactics >= NeckTacticsRequirement,
            HitLocation.Back => tactics >= BackTacticsRequirement,
            _                 => tactics >= GeneralLocationTacticsRequirement
        };
    }

    public static void SetLocation(Mobile attacker, HitLocation location) => Chosen[attacker] = location;

    /// <summary>
    ///     Reads (does NOT consume — see class doc) the attacker's current standing aim and
    ///     returns the percentage damage bonus/penalty it grants (matching the
    ///     percentageBonus scale already used in BaseWeapon.OnHit — can go negative if the
    ///     chosen slot turns out to be a bad match for the weapon in hand). Also rolls the
    ///     casting-disruption check against the defender's chosen casting channel (see
    ///     CastingChannelSystem) and any fracture chance.
    /// </summary>
    public static int ConsumeHitBonus(Mobile attacker, Mobile defender)
    {
        if (!Chosen.TryGetValue(attacker, out var location))
        {
            LastConsumed[attacker] = null; // no stance chosen — a plain hit, not aimed
            return 0;
        }

        if (!CanUseLocation(attacker, location))
        {
            LastConsumed[attacker] = null;
            return 0; // skill dropped below requirement since it was chosen — no bonus
        }

        LastConsumed[attacker] = location;

        CastingChannelSystem.TryDisruptCasting(defender, location);
        BoneFractureSystem.RollFracture(defender, location);

        if (location == HitLocation.Neck)
        {
            return 100; // flat x2, independent of armor — matches the original design
        }

        if (location == HitLocation.Head)
        {
            return 0; // no armor-profile bonus of its own — see the enum doc comment
        }

        if (attacker.Weapon is not BaseWeapon weapon)
        {
            return 0;
        }

        var piece = GetBestArmorPiece(defender, location);
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

        foreach (var location in LocationLayers.Keys)
        {
            if (location == HitLocation.Neck || location == HitLocation.Head || !CanUseLocation(attacker, location))
            {
                continue;
            }

            var piece = GetBestArmorPiece(defender, location);
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

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonHitLocationChoice", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(Chosen.Count);

            foreach (var (mobile, location) in Chosen)
            {
                writer.Write(mobile);
                writer.Write((byte)location);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version

            var count = reader.ReadEncodedInt();
            for (var i = 0; i < count; i++)
            {
                var mobile = reader.ReadEntity<Mobile>();
                var location = (HitLocation)reader.ReadByte();

                if (mobile != null)
                {
                    Chosen[mobile] = location;
                }
            }
        }
    }
}
