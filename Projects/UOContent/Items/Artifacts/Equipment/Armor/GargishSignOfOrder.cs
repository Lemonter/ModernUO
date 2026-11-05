using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/GargishSignOfOrder.cs).
///     GargishSignOfOrder: GargishOrderShield has no counterpart here; re-based on BaseShield with its ServUO art/stat line (`0x422A`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishSignOfOrder : BaseShield
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 23;

    public override int AosStrReq => 95;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 7.0;

    [Constructible]
    public GargishSignOfOrder()
        : base(0x422A)
    {
        SkillBonuses.SetValues(0, SkillName.Chivalry, 10.0);
        Attributes.AttackChance = 5;
        Attributes.DefendChance = 10;
        Attributes.CastSpeed = 1;
        Attributes.CastRecovery = 1;
    }

    public override int LabelNumber => 1113534; // Sign of Order

    public override int BasePhysicalResistance => 1;

    public override int BaseFireResistance => 0;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 5;

    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
