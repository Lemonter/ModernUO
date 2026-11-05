using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/LargeStoneShield.cs).
/// </summary>
// Based off a WoodenKiteShield
[Flippable(0x4205, 0x420B)]
[SerializationGenerator(0, false)]
public partial class LargeStoneShield : BaseShield
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public LargeStoneShield()
        : base(0x4205)
    {
        //Weight = 5.0;
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;
    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;
    public override int AosStrReq => 20;
    public override int ArmorBase => 12;
}
