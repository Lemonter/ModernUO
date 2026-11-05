using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Glasses/GargishGlasses.cs).
///     GargishGlasses: Dropped IRepairable — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishGlasses : BaseArmor
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishGlasses()
        : base(0x4644)
    {
        Layer = Layer.Earrings;
        Weight = 2;
    }

    public override int LabelNumber => 1096713; // Gargish Glasses

    public override int BasePhysicalResistance => 2;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 4;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 2;

    public override int InitMinHits => 36;

    public override int InitMaxHits => 48;

    public override int AosStrReq => 45;

    public override int OldStrReq => 40;

    public override int ArmorBase => 30;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;

    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
}
