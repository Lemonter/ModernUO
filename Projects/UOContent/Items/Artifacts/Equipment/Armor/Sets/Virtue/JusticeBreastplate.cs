using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtue/JusticeBreastplate.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class JusticeBreastplate : BaseArmor
{
    [Constructible]
    public JusticeBreastplate()
        : base(0x2B08)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 7.0;
        this.Hue = 0x226;

    }

    public override int LabelNumber => 1075190; // Breastplate of Justice (Virtue Armor Set)

    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 8;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int AosStrReq => 65;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
