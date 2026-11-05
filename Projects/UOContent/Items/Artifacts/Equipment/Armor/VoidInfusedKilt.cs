using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/VoidInfusedKilt.cs).
///     VoidInfusedKilt: GargishPlateKilt has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30C`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VoidInfusedKilt : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 32;

    public override int OldStrReq => 80;

    public override int AosStrReq => 80;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 5.0;

    public override int LabelNumber => 1113868; // Void Infused Kilt

    [Constructible]
    public VoidInfusedKilt()
        : base(0x30C)
    {
        Hue = 2124;
        Attributes.AttackChance = 5;
        Attributes.BonusStr = 5;
        Attributes.BonusDex = 5;
        Attributes.RegenMana = 1;
        Attributes.RegenStam = 1;
    }

    public override int BasePhysicalResistance => 13;

    public override int BaseFireResistance => 12;

    public override int BaseColdResistance => 8;

    public override int BasePoisonResistance => 9;

    public override int BaseEnergyResistance => 9;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
