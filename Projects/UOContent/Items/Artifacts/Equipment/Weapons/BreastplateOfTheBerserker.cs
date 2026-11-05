using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/BreastplateOfTheBerserker.cs).
///     BreastplateOfTheBerserker: GargishPlateChest has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30A`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BreastplateOfTheBerserker : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 36;

    public override int OldStrReq => 95;

    public override int AosStrReq => 95;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 10.0;

    public override int LabelNumber => 1113539; // Breastplate of the Berserker

    [Constructible]
    public BreastplateOfTheBerserker() : base(0x30A)
    {
        Hue = 1172;
        Attributes.WeaponSpeed = 10;
        Attributes.WeaponDamage = 15;
        Attributes.LowerManaCost = 4;
        Attributes.BonusHits = 5;
        Attributes.RegenStam = 3;
    }

    public override int BasePhysicalResistance => 18;

    public override int BaseFireResistance => 16;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 11;

    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
