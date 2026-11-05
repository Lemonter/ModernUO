using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/StandardOfChaosG.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StandardOfChaosG : DualPointedSpear
{
    public override int LabelNumber => 1113522; // Standard of Chaos

    [Constructible]
    public StandardOfChaosG()
    {
        Hue = 2209;
        WeaponAttributes.HitHarm = 30;
        WeaponAttributes.HitFireball = 20;
        WeaponAttributes.HitLightning = 10;
        WeaponAttributes.HitLowerDefend = 40;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = -40;
        Attributes.CastSpeed = 1;
        AosElementDamages.Chaos = 100;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
