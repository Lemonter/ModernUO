using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Mahaon mage ring artifact: half-mages (lower Magery) take 25% less damage,
///     full mages (high Magery) take 50% less damage. Computed live off the wearer's
///     current Magery skill rather than a fixed stat mod, so it stays correct if their
///     skill changes while the ring is worn.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MageRingOfProtection : BaseRing
{
    // Tune these against real Mahaon thresholds if remembered more precisely later.
    public const double FullMageThreshold = 70.0;
    public const double HalfMageThreshold = 30.0;

    [Constructible]
    public MageRingOfProtection() : base(0x108A)
    {
        Name = "кольцо мага";
        Hue = 0x489;
    }

    public override double DefaultWeight => 0.1;

    public static double GetDamageTakenScalar(Mobile m)
    {
        var ring = m.FindItemOnLayer<MageRingOfProtection>(Layer.Ring);
        if (ring == null)
        {
            return 1.0;
        }

        var magery = m.Skills[SkillName.Magery].Value;

        if (magery >= FullMageThreshold)
        {
            return 0.5;
        }

        if (magery >= HalfMageThreshold)
        {
            return 0.75;
        }

        return 1.0;
    }
}
