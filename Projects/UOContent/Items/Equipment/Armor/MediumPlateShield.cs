using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/MediumPlateShield.cs).
/// </summary>
// Based off a MetalShield
[Flippable(0x4203, 0x4209)]
[SerializationGenerator(0, false)]
public partial class MediumPlateShield : BaseShield
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public MediumPlateShield()
        : base(0x4203)
    {
        //Weight = 6.0;
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 1;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;
    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;
    public override int AosStrReq => 45;
    public override int ArmorBase => 11;
}
