using ModernUO.Serialization;
using System;
using Server.Guilds;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/GargishOrderShield.cs).
/// </summary>
// Based off an OrderShield
[Flippable(0x422A, 0x422C)]
[SerializationGenerator(0, false)]
public partial class GargishOrderShield : BaseShield
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishOrderShield()
        : base(0x422A)
    {
        if (!Core.AOS)
            this.LootType = LootType.Newbied;
        //Weight = 7.0;
    }

    public override int BasePhysicalResistance => 1;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 0;
    public override int InitMinHits => 100;
    public override int InitMaxHits => 125;
    public override int AosStrReq => 95;
    public override int ArmorBase => 30;

    public override bool OnEquip(Mobile from)
    {
        return this.Validate(from) && base.OnEquip(from);
    }

    public override void OnSingleClick(Mobile from)
    {
        if (this.Validate(this.Parent as Mobile))
            base.OnSingleClick(from);
    }

    public virtual bool Validate(Mobile m)
    {
        if (Core.AOS || m == null || !m.Player || m.AccessLevel != AccessLevel.Player)
            return true;

        Guild g = m.Guild as Guild;

        if (g == null || g.Type != GuildType.Order)
        {
            m.FixedEffect(0x3728, 10, 13);
            this.Delete();

            return false;
        }

        return true;
    }
}
