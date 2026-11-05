using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishAxe.cs).
/// </summary>
//Based off Axe
[Flippable(0x48B2, 0x48B3)]
[SerializationGenerator(0, false)]
public partial class GargishAxe : BaseAxe
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishAxe()
        : base(0x48B2)
    {
        this.Weight = 4.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;
    public override int AosStrengthReq => 35;
    public override int AosMinDamage => 14;
    public override int AosMaxDamage => 17;
    public override int AosSpeed => 37;
    public override float MlSpeed => 3.00f;
    public override int OldStrengthReq => 35;
    public override int OldMinDamage => 6;
    public override int OldMaxDamage => 33;
    public override int OldSpeed => 37;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 110;
}
