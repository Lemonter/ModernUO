using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/GargishChaosShield.cs).
/// </summary>
[Flippable(0x4228, 0x4229)]
[SerializationGenerator(0, false)]
public partial class GargishChaosShield : BaseShield
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int BasePhysicalResistance => 1;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;
    public override int InitMinHits => 100;
    public override int InitMaxHits => 125;
    public override int AosStrReq => 95;
    public override int ArmorBase => 32;

    [Constructible]
    public GargishChaosShield()
        : base(0x4228)
    {
        Weight = 5.0;
    }
}
