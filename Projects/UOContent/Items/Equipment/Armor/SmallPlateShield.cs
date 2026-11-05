using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/SmallPlateShield.cs).
/// </summary>
// Based off a BronzeShield
[Flippable(0x4202, 0x420A)]
[SerializationGenerator(0, false)]
public partial class SmallPlateShield : BaseShield
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public SmallPlateShield()
        : base(0x4202)
    {
        this.Weight = 6.0;
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 1;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;
    public override int InitMinHits => 25;
    public override int InitMaxHits => 30;
    public override int AosStrReq => 35;
    public override int ArmorBase => 10;
}
