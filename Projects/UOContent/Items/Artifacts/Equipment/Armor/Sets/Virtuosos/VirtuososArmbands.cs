using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtuosos/VirtuososArmbands.cs).
///     VirtuososArmbands: GargishPlateArms has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x308`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VirtuososArmbands : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 32;

    public override int OldStrReq => 80;

    public override int AosStrReq => 80;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 5.0;

    public override int LabelNumber => 1151558; // Virtuoso's Armbands

    public override int BasePhysicalResistance => 24;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 9;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 9;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public VirtuososArmbands() : base(0x308)
    {
        this.Hue = 1374;
        this.Weight = 5;
        this.StrRequirement = 80;
    }
}
