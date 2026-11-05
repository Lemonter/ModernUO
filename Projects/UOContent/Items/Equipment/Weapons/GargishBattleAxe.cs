using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishBattleAxe.cs).
/// </summary>
//Based Off Battle Axe
[Flippable(0x48B0, 0x48B1)]
[SerializationGenerator(0, false)]
public partial class GargishBattleAxe : BaseAxe
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishBattleAxe()
        : base(0x48B0)
    {
        this.Weight = 4.0;
        this.Layer = Layer.TwoHanded;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;
    public override int AosStrengthReq => 35;
    public override int AosMinDamage => 16;
    public override int AosMaxDamage => 19;
    public override int AosSpeed => 31;
    public override float MlSpeed => 3.50f;
    public override int OldStrengthReq => 40;
    public override int OldMinDamage => 6;
    public override int OldMaxDamage => 38;
    public override int OldSpeed => 30;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 70;
}
