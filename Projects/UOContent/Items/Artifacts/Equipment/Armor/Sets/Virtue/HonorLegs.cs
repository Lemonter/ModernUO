using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtue/HonorLegs.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HonorLegs : BaseArmor
{
    [Constructible]
    public HonorLegs()
        : base(0x2B06)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 9.0;
        this.Hue = 0x226;

    }

    public override int LabelNumber => 1075193; // Legs of Honor (Virtue Armor Set)

    public override int BasePhysicalResistance => 8;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int AosStrReq => 70;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
