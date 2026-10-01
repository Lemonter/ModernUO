using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>What Niporailem the Thief throws at you: a 100-stone sack of his gold, forced into
/// your pack so you can barely move. Ported from ServUO (Scripts/Items/Quest/FoolishGold.cs).
///
/// Putting it down anywhere at all turns it into worthless treasure sand at a quarter the
/// weight, and it decays in fifteen minutes — unless it is still linked to a living
/// Niporailem, in which case it stays until the fight is over.</summary>
[SerializationGenerator(0, false)]
public partial class NiporailemsTreasure : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _link;

    [Constructible]
    public NiporailemsTreasure(Mobile link = null) : base(0xEEF)
    {
        _link = link;
        Weight = 100.0;
    }

    // Niporailem's Treasure : Treasure Sand
    public override int LabelNumber => ItemID == 0x11EA ? 1112115 : 1112113;

    public override bool Decays => _link?.Deleted != false || base.Decays;

    public override TimeSpan DecayTime => TimeSpan.FromMinutes(15);

    public override bool DropToWorld(Mobile from, Point3D p)
    {
        var dropped = base.DropToWorld(from, p);

        if (dropped)
        {
            ConvertItem(from);
        }

        return dropped;
    }

    public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
    {
        var dropped = base.DropToMobile(from, target, p);

        if (dropped)
        {
            ConvertItem(from);
        }

        return dropped;
    }

    public override bool DropToItem(Mobile from, Item target, Point3D p)
    {
        var dropped = base.DropToItem(from, target, p);

        if (dropped && Parent != from.Backpack)
        {
            ConvertItem(from);
        }

        return dropped;
    }

    public virtual void ConvertItem(Mobile from)
    {
        from.SendLocalizedMessage(1112112); // To carry the burden of greed!

        ItemID = 0x11EA;
        Weight = 25.0;
    }
}
