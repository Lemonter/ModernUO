using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/LargePlateShield.cs).
/// </summary>
// Based off a HeaterShield
[Flippable(0x4204, 0x4208)]
[SerializationGenerator(0, false)]
public partial class LargePlateShield : BaseShield
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public LargePlateShield()
        : base(0x4204)
    {
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 1;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;
    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;
    public override int AosStrReq => 90;
    public override int ArmorBase => 23;
}
