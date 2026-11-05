using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/LegacyOfDespair.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LegacyOfDespair : DreadSword
{
    public override int LabelNumber => 1113519; // Legacy of Despair

    [Constructible]
    public LegacyOfDespair()
    {
        Hue = 48;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 60;
        WeaponAttributes.HitLowerDefend = 50;
        WeaponAttributes.HitLowerAttack = 50;
        AosElementDamages.Cold = 75;
        AosElementDamages.Poison = 25;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
