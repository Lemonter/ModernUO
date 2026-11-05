using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/StaffRing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StaffRing : BaseRing
{
    [Constructible]
    public StaffRing()
        : base(0x108a)
    {
        Name = "кольцо персонала";
        Attributes.NightSight = 1;
        Attributes.AttackChance = 20;
        Attributes.LowerRegCost = 100;
        Attributes.LowerManaCost = 100;
        Attributes.RegenHits = 12;
        Attributes.RegenStam = 24;
        Attributes.RegenMana = 18;
        Attributes.SpellDamage = 30;
        Attributes.CastRecovery = 6;
        Attributes.CastSpeed = 4;
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Player)
        {
            from.SendMessage("Этот предмет доступен только персоналу.");
            this.Delete();
        }
    }

    public override bool OnEquip(Mobile from)
    {
        if (from.Player)
        {
            from.SendMessage("Этот предмет доступен только персоналу.");
            this.Delete();
        }
        return true;
    }
}
