using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/StoneDragonsTooth.cs).
///     StoneDragonsTooth: GargishDagger has no counterpart here; re-based on BaseKnife with its ServUO art/stat line (`0x902`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StoneDragonsTooth : BaseKnife
{
    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1113523; // Stone Dragon's Tooth

    [Constructible]
    public StoneDragonsTooth()
        : base(0x902)
    {
        Hue = 2407;
        Attributes.WeaponSpeed = 10;
        Attributes.WeaponDamage = 50;
        Attributes.RegenHits = 3;
        WeaponAttributes.HitMagicArrow = 40;
        WeaponAttributes.HitLowerDefend = 30;
        WeaponAttributes.ResistFireBonus = 10;
        AosElementDamages.Poison = 100;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
