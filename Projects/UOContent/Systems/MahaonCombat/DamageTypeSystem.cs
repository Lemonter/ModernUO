using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     The three physical damage types and the piercing/blunt/slashing split every weapon
///     and armor piece gets classified into. Classified by weapon/armor family (there's no
///     per-item hand-tuning here — every dagger behaves like every other dagger) since
///     covering every individual item file by hand isn't practical. Percentages are my own
///     calls, not sourced from anything — reasonable is the bar, not authoritative.
/// </summary>
public readonly struct DamageProfile
{
    public readonly double Piercing;
    public readonly double Blunt;
    public readonly double Slashing;

    public DamageProfile(double piercing, double blunt, double slashing)
    {
        Piercing = piercing;
        Blunt = blunt;
        Slashing = slashing;
    }
}

public static class DamageTypeSystem
{
    // -- Weapon profiles (fractions of 1.0, sum to 1.0) -----------------------------------

    public static DamageProfile GetWeaponProfile(BaseWeapon weapon) => weapon switch
    {
        BaseKnife    => new DamageProfile(0.70, 0.00, 0.30), // thrust-heavy, a little edge
        BaseSpear    => new DamageProfile(0.70, 0.10, 0.20), // thrusting polearms
        BasePoleArm  => new DamageProfile(0.40, 0.15, 0.45), // halberds etc — chop and thrust both
        BaseAxe      => new DamageProfile(0.05, 0.25, 0.70), // heavy chopping edge
        BaseSword    => new DamageProfile(0.10, 0.10, 0.80), // pure edge
        BaseBashing  => new DamageProfile(0.05, 0.90, 0.05), // maces, hammers, clubs
        BaseRanged   => new DamageProfile(0.80, 0.00, 0.20), // arrows/bolts
        _            => new DamageProfile(0.10, 0.70, 0.20)  // fists/wrestling, unarmed default
    };

    // -- Armor profiles (fractions of 1.0, sum to 1.0) -------------------------------------

    public static DamageProfile GetArmorProfile(BaseArmor armor)
    {
        var name = armor.GetType().Name;

        if (name.Contains("Plate"))
        {
            return new DamageProfile(0.35, 0.15, 0.50); // deflects blades, weak to blunt trauma
        }

        if (name.Contains("Chain"))
        {
            return new DamageProfile(0.30, 0.15, 0.55); // rings stop cuts, crush under blows
        }

        if (name.Contains("Ring"))
        {
            return new DamageProfile(0.25, 0.25, 0.50);
        }

        if (name.Contains("Studded"))
        {
            return new DamageProfile(0.30, 0.30, 0.40);
        }

        if (name.Contains("Bone"))
        {
            return new DamageProfile(0.35, 0.20, 0.45); // rigid plates, similar profile to plate-lite
        }

        if (name.Contains("Dragon"))
        {
            return new DamageProfile(0.40, 0.30, 0.30); // magically tough, balanced
        }

        if (name.Contains("Leather"))
        {
            return new DamageProfile(0.30, 0.35, 0.35);
        }

        // Cloth and anything unclassified — padded, barely stops an edge or a point.
        return new DamageProfile(0.20, 0.40, 0.40);
    }

    /// <summary>Bare skin: no resistance to anything, all three types get through in full.</summary>
    public static readonly DamageProfile NoArmor = new(0.0, 0.0, 0.0);

    /// <summary>
    ///     How much of the weapon's damage distribution overlaps with the target's
    ///     resistance distribution — 0 means the weapon's damage types sail through
    ///     completely untouched (a real breach), 1 means every point of damage lands on
    ///     the target's strongest resistance.
    /// </summary>
    public static double GetOverlap(DamageProfile weapon, DamageProfile armor) =>
        weapon.Piercing * armor.Piercing + weapon.Blunt * armor.Blunt + weapon.Slashing * armor.Slashing;
}
