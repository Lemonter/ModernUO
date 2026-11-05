using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/ClawsOfTheBerserker.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ClawsOfTheBerserker : Tekagi
{
    public override int LabelNumber => 1113758; // Claws of the Berserker

    [Constructible]
    public ClawsOfTheBerserker()
        : base()
    {
        Hue = 1172;
        WeaponAttributes.HitLightning = 45;
        WeaponAttributes.HitLowerDefend = 50;
        Attributes.CastSpeed = 1;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 60;
    }

    public override int InitMinHits => 35;

    public override int InitMaxHits => 60;
}
