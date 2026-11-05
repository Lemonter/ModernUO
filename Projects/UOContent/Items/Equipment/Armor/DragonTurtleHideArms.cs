using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/DragonTurtleHideArms.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonTurtleHideArms : BaseArmor
{
    public override int BasePhysicalResistance => 3;
    public override int BaseFireResistance => 3;
    public override int BaseColdResistance => 4;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 2;

    public override int InitMinHits => 35;
    public override int InitMaxHits => 45;

    public override int AosStrReq => 30;
    public override int OldStrReq => 20;

    public override int ArmorBase => 15;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override int LabelNumber => 1109638; // Dragon Turtle Hide Arms

    [Constructible]
    public DragonTurtleHideArms()
        : base(0x782E)
    {
        Weight = 3.0;
    }
}
