using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishDaisho.cs).
/// </summary>
//Bassed Off Daisho
[Flippable(0x48D0, 0x48D1)]
[SerializationGenerator(0, false)]
public partial class GargishDaisho : BaseSword
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishDaisho()
        : base(0x48D0)
    {
        this.Weight = 8.0;
        this.Layer = Layer.TwoHanded;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.Feint;
    public override WeaponAbility SecondaryAbility => WeaponAbility.DoubleStrike;
    public override int AosStrengthReq => 40;
    public override int AosMinDamage => 13;
    public override int AosMaxDamage => 16;
    public override int AosSpeed => 40;
    public override float MlSpeed => 2.75f;
    public override int OldStrengthReq => 40;
    public override int OldMinDamage => 13;
    public override int OldMaxDamage => 15;
    public override int OldSpeed => 40;
    public override int DefHitSound => 0x23B;
    public override int DefMissSound => 0x23A;
    public override int InitMinHits => 45;
    public override int InitMaxHits => 65;
}
