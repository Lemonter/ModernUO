using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/LifeSyphon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LifeSyphon : BloodBlade
{
    public override int LabelNumber => 1113524; // Life Syphon

    [Constructible]
    public LifeSyphon()
        : base()
    {
        Hue = 1172;
        WeaponAttributes.HitHarm = 30;
        WeaponAttributes.HitLeechHits = 100;
        Attributes.BonusHits = 10;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 50;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
