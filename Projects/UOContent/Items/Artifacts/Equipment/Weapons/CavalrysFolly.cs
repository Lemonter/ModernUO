using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/CavalrysFolly.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CavalrysFolly : BladedStaff
{
    public override int LabelNumber => 1115446; // Cavalry's Folly

    [Constructible]
    public CavalrysFolly()
        : base()
    {
        Hue = 1165;
        Attributes.BonusHits = 2;
        Attributes.AttackChance = 10;
        Attributes.WeaponDamage = 45;
        Attributes.WeaponSpeed = 35;
        WeaponAttributes.HitLowerDefend = 40;
        WeaponAttributes.HitFireball = 40;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
