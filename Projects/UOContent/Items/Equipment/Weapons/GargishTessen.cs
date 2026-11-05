using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishTessen.cs).
/// </summary>
//Based Of Tessen
[Flippable(0x48CC, 0x48CD)]
[SerializationGenerator(0, false)]
public partial class GargishTessen : BaseBashing
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishTessen()
        : base(0x48CC)
    {
        this.Weight = 6.0;
        this.Layer = Layer.TwoHanded;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.Feint;
    public override WeaponAbility SecondaryAbility => WeaponAbility.DualWield;
    public override int AosStrengthReq => 10;
    public override int AosMinDamage => 10;
    public override int AosMaxDamage => 13;
    public override int AosSpeed => 50;
    public override float MlSpeed => 2.00f;
    public override int OldStrengthReq => 10;
    public override int OldMinDamage => 10;
    public override int OldMaxDamage => 12;
    public override int OldSpeed => 50;
    public override int DefHitSound => 0x232;
    public override int DefMissSound => 0x238;
    public override int InitMinHits => 55;
    public override int InitMaxHits => 60;
    public override WeaponAnimation DefAnimation => WeaponAnimation.Bash2H;
}
