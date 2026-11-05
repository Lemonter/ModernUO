using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishPike.cs).
/// </summary>
//Based Off Pike
[Flippable(0x48C8, 0x48C9)]
[SerializationGenerator(0, false)]
public partial class GargishPike : BaseSpear
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishPike()
        : base(0x48C8)
    {
        this.Weight = 8.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ParalyzingBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;
    public override int AosStrengthReq => 50;
    public override int AosMinDamage => 14;
    public override int AosMaxDamage => 17;
    public override int AosSpeed => 37;
    public override float MlSpeed => 3.00f;
    public override int OldStrengthReq => 50;
    public override int OldMinDamage => 14;
    public override int OldMaxDamage => 16;
    public override int OldSpeed => 37;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 110;
}
