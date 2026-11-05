using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/GargishWarHammer.cs).
/// </summary>
//Based Off War Hammer
[Flippable(0x48C0, 0x481)]
[SerializationGenerator(0, false)]
public partial class GargishWarHammer : BaseBashing
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishWarHammer()
        : base(0x48C0)
    {
        this.Weight = 10.0;
        this.Layer = Layer.TwoHanded;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
    public override WeaponAbility SecondaryAbility => WeaponAbility.CrushingBlow;
    public override int AosStrengthReq => 95;
    public override int AosMinDamage => 17;
    public override int AosMaxDamage => 20;
    public override int AosSpeed => 28;
    public override float MlSpeed => 3.75f;
    public override int OldStrengthReq => 40;
    public override int OldMinDamage => 8;
    public override int OldMaxDamage => 36;
    public override int OldSpeed => 31;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 110;
    public override WeaponAnimation DefAnimation => WeaponAnimation.Bash2H;
}
