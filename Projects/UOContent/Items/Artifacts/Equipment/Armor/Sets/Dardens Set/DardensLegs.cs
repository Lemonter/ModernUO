using ModernUO.Serialization;
using System;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Dardens Set/DardensLegs.cs).
///     DardensLegs: DragonTurtleHideLegs has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x782D`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DardensLegs : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;

    public override int ArmorBase => 16;

    public override int OldStrReq => 35;

    public override int AosStrReq => 30;

    public override int InitMaxHits => 45;

    public override int InitMinHits => 35;

    public override double DefaultWeight => 6.0;

    public override int LabelNumber => 1156242; // Darden's Armor

    public override int BasePhysicalResistance => 6;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    [Constructible]
    public DardensLegs() : base(0x782D)
    {
        Attributes.BonusStr = 4;
        Attributes.BonusHits = 4;
        Attributes.LowerRegCost = 15;

    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1156346); // Myrmidex Slayer
    }
}
