using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Bestial/BestialNecklace.cs).
///     BestialNecklace: GargishNecklace has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x4210`).
///     BestialNecklace: Dropped ISetItem — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BestialNecklace : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 11;

    public override Layer Layer => Layer.Neck;

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1151544; // Bestial Necklace

    public override int BasePhysicalResistance => 3;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 4;

    public override int BasePoisonResistance => 4;

    public override int BaseEnergyResistance => 17;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public BestialNecklace() : base(0x4210)
    {
        this.Hue = 2010;
        this.Weight = 1;
    }
}
