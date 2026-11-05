using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/InfusedGlassStave.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class InfusedGlassStave : BaseStaff
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1112909; // infused glass stave

    [Constructible]
    public InfusedGlassStave()
        : base(0x905)
    {
        Weight = 4.0;
        Hue = 23;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;

    public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

    public override int AosStrengthReq => 20;

    public override int AosMinDamage => 11;

    public override int AosMaxDamage => 14;

    public override int AosSpeed => 39;

    public override float MlSpeed => 2.25f;

    public override int OldStrengthReq => 35;

    public override int OldMinDamage => 8;

    public override int OldMaxDamage => 33;

    public override int OldSpeed => 35;

    public override int InitMinHits => 31;

    public override int InitMaxHits => 70;
}
