using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/DragonTurtleHideHelm.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonTurtleHideHelm : BaseArmor
{
    public override int BasePhysicalResistance => 1;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 2;
    public override int BasePoisonResistance => 2;
    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 20;
    public override int InitMaxHits => 35;

    public override int AosStrReq => 30;
    public override int OldStrReq => 10;

    public override int ArmorBase => 30;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override int LabelNumber => 1109637; // Dragon Turtle Hide Helm

    [Constructible]
    public DragonTurtleHideHelm()
        : base(0x782D)
    {
        Weight = 2.0;
    }
}
