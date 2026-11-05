using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishButcherKnife.cs).
/// </summary>
//Based Off Butcher Knife
[Flippable(0x48B6, 0x48B7)]
[SerializationGenerator(0, false)]
public partial class GargishButcherKnife : BaseKnife
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishButcherKnife()
        : base(0x48B6)
    {
        Weight = 1.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.InfectiousStrike;
    public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;
    public override int AosStrengthReq => 10;
    public override int AosMinDamage => 10;
    public override int AosMaxDamage => 13;
    public override int AosSpeed => 49;
    public override float MlSpeed => 2.25f;
    public override int OldStrengthReq => 5;
    public override int OldMinDamage => 2;
    public override int OldMaxDamage => 14;
    public override int OldSpeed => 40;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 40;
}
