using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/RunedDriftwoodBow.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RunedDriftwoodBow : Bow
{
    public override int LabelNumber => 1149961;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public RunedDriftwoodBow()
    {
        Hue = 2955;
        WeaponAttributes.HitLightning = 40;
        WeaponAttributes.HitLowerDefend = 40;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 50;
    }
}
