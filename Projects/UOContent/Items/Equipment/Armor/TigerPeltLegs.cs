using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/TigerPeltLegs.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TigerPeltLegs : BaseArmor
{
    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 3;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 3;

    public override int InitMinHits => 30;
    public override int InitMaxHits => 40;

    public override int AosStrReq => 25;
    public override int OldStrReq => 10;

    public override int ArmorBase => 13;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override bool AllowMaleWearer => true;

    public override int LabelNumber => 1109628; // Tiger Pelt Leggins

    [Constructible]
    public TigerPeltLegs()
        : base(0x7824)
    {
        Weight = 2.0;
    }
}
