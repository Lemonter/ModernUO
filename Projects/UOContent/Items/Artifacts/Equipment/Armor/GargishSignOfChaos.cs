using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/GargishSignOfChaos.cs).
///     GargishSignOfChaos: GargishChaosShield has no counterpart here; re-based on BaseShield with its ServUO art/stat line (`0x4228`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishSignOfChaos : BaseShield
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 32;

    public override int AosStrReq => 95;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 5.0;

    [Constructible]
    public GargishSignOfChaos()
        : base(0x4228)
    {
        Hue = 2075;
        Attributes.AttackChance = 5;
        Attributes.DefendChance = 10;
        Attributes.CastSpeed = 1;
    }

    public override int LabelNumber => 1113535; // Sign of Chaos

    public override int BasePhysicalResistance => 3;

    public override int BaseFireResistance => 2;

    public override int BaseColdResistance => 2;

    public override int BasePoisonResistance => 2;

    public override int BaseEnergyResistance => 2;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
