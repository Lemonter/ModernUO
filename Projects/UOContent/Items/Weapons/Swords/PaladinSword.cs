using ModernUO.Serialization;

namespace Server.Items;

// Graphic reused from VikingSword — no primary-source OSI graphic ID found this session.
[SerializationGenerator(0, false)]
public partial class PaladinSword : BaseSword
{
    [Constructible]
    public PaladinSword() : base(0x13B9)
    {
    }

    public override double DefaultWeight => 6.0;

    public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
    public override WeaponAbility SecondaryAbility => WeaponAbility.CrushingBlow;

    public override int AosStrengthReq => 35;
    public override int AosMinDamage => 15;
    public override int AosMaxDamage => 17;
    public override int AosSpeed => 30;
    public override float MlSpeed => 3.00f;

    public override int OldStrengthReq => 15;
    public override int OldMinDamage => 5;
    public override int OldMaxDamage => 26;
    public override int OldSpeed => 30;

    public override int InitMinHits => 30;
    public override int InitMaxHits => 60;

    public override int DefHitSound => 0x237;
    public override int DefMissSound => 0x23A;
}
