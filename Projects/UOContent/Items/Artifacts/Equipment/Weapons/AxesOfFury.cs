using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/AxesOfFury.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AxesOfFury : DualShortAxes
{
    public override int LabelNumber => 1113517; // Axes Of Fury

    [Constructible]
    public AxesOfFury()
    {
        Hue = 33;
        WeaponAttributes.HitFireball = 45;
        WeaponAttributes.HitLowerDefend = 40;
        Attributes.BonusDex = 5;
        Attributes.DefendChance = -15;
        Attributes.AttackChance = 20;
        Attributes.WeaponDamage = 45;
        Attributes.WeaponSpeed = 30;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
