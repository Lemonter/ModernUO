using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/FemaleGargishPlateArms.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FemaleGargishPlateArms : BaseArmor
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public FemaleGargishPlateArms()
        : this(0)
    {
    }

    public FemaleGargishPlateArms(int hue)
        : base(0x307)
    {
        Weight = 5.0;
        Hue = hue;
    }

    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 6;
    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;

    public override int AosStrReq => 80;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
