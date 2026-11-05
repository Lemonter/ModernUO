using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/BladedWhip.cs).
///     ServUO's IRepairable / RepairSystem members are dropped: IRepairable does not exist here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BladedWhip : BaseSword
{
    public override int LabelNumber => 1125643; // Bladed Whip

    [Constructible]
    public BladedWhip() : base(0xA28B)
    {
        Weight = 5.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;

    public override WeaponAbility SecondaryAbility => WeaponAbility.WhirlwindAttack;

    public override int AosStrengthReq => 20;

    public override int AosMinDamage => 13;

    public override int AosMaxDamage => 17;

    public override float MlSpeed => 3.25f;

    public override int DefHitSound => 0x23B;

    public override int DefMissSound => 0x23A;

    public override int InitMinHits => 30;

    public override int InitMaxHits => 60;
}
