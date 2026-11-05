using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishBoneHarvester.cs).
/// </summary>
//Based off Bone Harvester
[Flippable(0x48C6, 0x48C7)]
[SerializationGenerator(0, false)]
public partial class GargishBoneHarvester : BaseSword
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishBoneHarvester()
        : base(0x48C6)
    {
        this.Weight = 3.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ParalyzingBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;
    public override int AosStrengthReq => 25;
    public override int AosMinDamage => 12;
    public override int AosMaxDamage => 16;
    public override int AosSpeed => 36;
    public override float MlSpeed => 3.00f;
    public override int OldStrengthReq => 25;
    public override int OldMinDamage => 13;
    public override int OldMaxDamage => 15;
    public override int OldSpeed => 36;
    public override int DefHitSound => 0x23B;
    public override int DefMissSound => 0x23A;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 70;
}
