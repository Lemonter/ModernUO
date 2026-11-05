using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/PillarOfStrength.cs).
///     PillarOfStrength: LargeStoneShield has no counterpart here; re-based on BaseShield with its ServUO art/stat line (`0x4205`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PillarOfStrength : BaseShield
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Stone;

    public override int ArmorBase => 26;

    public override int AosStrReq => 60;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 6.0;

    public override int LabelNumber => 1113533;

    [Constructible]
    public PillarOfStrength()
        : base(0x4205)
    {
        Attributes.BonusStr = 10;
        Attributes.BonusHits = 10;
        Attributes.WeaponDamage = 20;
    }

    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 0;

    public override int BaseColdResistance => 0;

    public override int BasePoisonResistance => 1;

    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
