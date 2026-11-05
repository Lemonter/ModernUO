using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/DragonHideShield.cs).
///     DragonHideShield: GargishKiteShield has no counterpart here; re-based on BaseShield with its ServUO art/stat line (`0x4201`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonHideShield : BaseShield
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Wood;

    public override int ArmorBase => 23;

    public override int AosStrReq => 45;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 7.0;

    [Constructible]
    public DragonHideShield()
        : base(0x4201)
    {
        Hue = 44;
        Attributes.RegenHits = 2;
        Attributes.DefendChance = 10;
    }

    public override int LabelNumber => 1113532; // Dragon Hide Shield

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 0;

    public override int BaseColdResistance => 0;

    public override int BasePoisonResistance => 0;

    public override int BaseEnergyResistance => -4;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
