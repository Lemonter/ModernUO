using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/DemonHuntersStandard.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DemonHuntersStandard : Spear
{
    public override int LabelNumber => 1113864; // Demon Hunter's Standard

    [Constructible]
    public DemonHuntersStandard()
    {
        Hue = 1377;
        Attributes.CastSpeed = 1;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.HitLeechStam = 50;
        WeaponAttributes.HitLightning = 40;
        WeaponAttributes.HitLowerDefend = 30;
        Slayer = SlayerName.Exorcism;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
