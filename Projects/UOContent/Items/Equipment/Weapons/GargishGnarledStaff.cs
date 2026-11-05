using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishGnarledStaff.cs).
/// </summary>
//Based Off Gnarled Staff
[Flippable(0x48B8, 0x48B9)]
[SerializationGenerator(0, false)]
public partial class GargishGnarledStaff : BaseStaff
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishGnarledStaff()
        : base(0x48B8)
    {
        this.Weight = 3.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ConcussionBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ForceOfNature;
    public override int AosStrengthReq => 20;
    public override int AosMinDamage => 15;
    public override int AosMaxDamage => 18;
    public override int AosSpeed => 33;
    public override float MlSpeed => 3.25f;
    public override int OldStrengthReq => 20;
    public override int OldMinDamage => 10;
    public override int OldMaxDamage => 30;
    public override int OldSpeed => 33;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 50;
}
