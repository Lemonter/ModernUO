using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/GargishKiteShield.cs).
/// </summary>
// Based off a MetalKiteShield
[Flippable(0x4201, 0x4206)]
[SerializationGenerator(0, false)]
public partial class GargishKiteShield : BaseShield, IDyable
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishKiteShield()
        : base(0x4201)
    {
        //Weight = 7.0;
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;
    public override int InitMinHits => 45;
    public override int InitMaxHits => 60;
    public override int AosStrReq => 45;
    public override int ArmorBase => 16;
    public bool Dye(Mobile from, DyeTub sender)
    {
        if (this.Deleted)
            return false;

        this.Hue = sender.DyedHue;

        return true;
    }
}
