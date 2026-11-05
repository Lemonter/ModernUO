using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishKryss.cs).
/// </summary>
[Flippable(0x48BC, 0x48BD)]
[SerializationGenerator(0, false)]
public partial class GargishKryss : BaseSword
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishKryss()
        : base(0x48BC)
    {
        Weight = 2.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
    public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;
    public override int AosStrengthReq => 10;
    public override int AosMinDamage => 10;
    public override int AosMaxDamage => 12;
    public override int AosSpeed => 53;
    public override float MlSpeed => 2.00f;
    public override int OldStrengthReq => 10;
    public override int OldMinDamage => 3;
    public override int OldMaxDamage => 28;
    public override int OldSpeed => 53;
    public override int DefHitSound => 0x23C;
    public override int DefMissSound => 0x238;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 90;
    public override SkillName DefSkill => SkillName.Fencing;
    public override WeaponType DefType => WeaponType.Piercing;
    public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;
}
