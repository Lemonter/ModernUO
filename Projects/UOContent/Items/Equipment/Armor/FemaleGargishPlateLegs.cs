using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/FemaleGargishPlateLegs.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FemaleGargishPlateLegs : BaseArmor
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public FemaleGargishPlateLegs()
        : this(0)
    {
    }

    public FemaleGargishPlateLegs(int hue)
        : base(0x30D)
    {
        Weight = 7.0;
        Hue = hue;
    }

    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 6;
    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;

    public override int AosStrReq => 90;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
