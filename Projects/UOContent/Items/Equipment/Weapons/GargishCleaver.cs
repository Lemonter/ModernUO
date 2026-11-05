using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishCleaver.cs).
/// </summary>
//Based off Cleaver
[Flippable(0x48AE, 0x48AF)]
[SerializationGenerator(0, false)]
public partial class GargishCleaver : BaseKnife
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishCleaver()
        : base(0x48AE)
    {
        Weight = 2.0;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
    public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;
    public override int AosStrengthReq => 10;
    public override int AosMinDamage => 10;
    public override int AosMaxDamage => 14;
    public override int AosSpeed => 46;
    public override float MlSpeed => 2.50f;
    public override int OldStrengthReq => 10;
    public override int OldMinDamage => 2;
    public override int OldMaxDamage => 13;
    public override int OldSpeed => 40;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 50;
}
