using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/DragonTurtleHideChest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonTurtleHideChest : BaseArmor
{
    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 3;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 4;

    public override int InitMinHits => 35;
    public override int InitMaxHits => 45;

    public override int AosStrReq => 30;
    public override int OldStrReq => 35;

    public override int ArmorBase => 16;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override int LabelNumber => 1109634; // Dragon Turtle Hide Chest

    [Constructible]
    public DragonTurtleHideChest()
        : base(0x782A)
    {
        Weight = 8.0;
    }
}
