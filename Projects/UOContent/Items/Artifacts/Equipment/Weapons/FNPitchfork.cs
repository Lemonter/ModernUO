using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/FNPitchfork.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FNPitchfork : BaseSpear
{
    [Constructible]
    public FNPitchfork()
        : base(0xE87)
    {
        Weight = 11.0;
    }

    public override int LabelNumber => 1113498; // Farmer Nash's Pitchfork

    public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;

    public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

    public override int AosStrengthReq => 50;

    public override int AosMinDamage => 13;

    public override int AosMaxDamage => 14;

    public override int AosSpeed => 43;

    public override float MlSpeed => 2.50f;

    public override int OldStrengthReq => 15;

    public override int OldMinDamage => 4;

    public override int OldMaxDamage => 16;

    public override int OldSpeed => 45;

    public override int InitMinHits => 31;

    public override int InitMaxHits => 60;
}
