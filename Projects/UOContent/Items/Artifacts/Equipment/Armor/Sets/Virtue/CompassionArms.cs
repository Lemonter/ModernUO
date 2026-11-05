using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtue/CompassionArms.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CompassionArms : BaseArmor
{
    [Constructible]
    public CompassionArms()
        : base(0x2B0A)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 3.0;
        this.Hue = 0x226;

    }

    public override int LabelNumber => 1075191; // Arms of Compassion (Virtue Armor Set)

    public override int BasePhysicalResistance => 8;

    public override int BaseFireResistance => 11;

    public override int BaseColdResistance => 6;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int AosStrReq => 60;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
