using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/AxeOfAbandon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AxeOfAbandon : BattleAxe
{
    public override int LabelNumber => 1113863; // Axe of Abandon

    [Constructible]
    public AxeOfAbandon()
    {
        Hue = 556;
        WeaponAttributes.HitLowerDefend = 40;
        Attributes.AttackChance = 15;
        Attributes.DefendChance = 10;
        Attributes.CastSpeed = 1;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
