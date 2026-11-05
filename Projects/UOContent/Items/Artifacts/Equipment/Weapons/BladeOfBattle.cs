using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/BladeOfBattle.cs).
///     BladeOfBattle: Shortblade has no counterpart here; re-based on BaseSword with its ServUO art/stat line (`0x907`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BladeOfBattle : BaseSword
{
    public override double DefaultWeight => 9.0;

    public override int LabelNumber => 1113525; // Blade of Battle

    [Constructible]
    public BladeOfBattle() : base(0x907)
    {
        Hue = 2045;
        WeaponAttributes.HitLowerDefend = 40;
        Attributes.AttackChance = 15;
        Attributes.DefendChance = 10;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 50;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
