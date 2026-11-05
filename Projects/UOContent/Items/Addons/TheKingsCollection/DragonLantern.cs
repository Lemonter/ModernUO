using ModernUO.Serialization;
using System;
using Server.Multis;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/DragonLantern.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonLantern : BaseLight, IAddon, IChoppable
{
    public override int LabelNumber => 1098392; // dragon lantern

    public override int LitItemID => ItemID == 0x4C40 ? 0x4C41 : 0x4C43;
    public override int UnlitItemID => ItemID == 0x4C41 ? 0x4C40 : 0x4C42;

    public int NorthID => Burning ? 0x4C41 : 0x4C40;
    public int WestID => Burning ? 0x4C43 : 0x4C42;

    [Constructible]
    public DragonLantern()
        : base(0x4C40)
    {
        Movable = false;
        Duration = TimeSpan.Zero;
        Burning = false;
        Light = LightType.Circle300;
        Weight = 0.0;
    }

    public Item Deed => new DragonLanternDeed();

    public bool CouldFit(IPoint3D p, Map map)
    {
        return map.CanFit((Point3D)p, 20);
    }

    void IChoppable.OnChop(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house != null && (house.IsOwner(from) || (house.Addons.Contains(this) && house.IsCoOwner(from))))
        {
            Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.

            Delete();

            house.Addons.Remove(this);

            from.AddToBackpack(Deed);
        }
        else
        {
            from.SendLocalizedMessage(1113134); // You can only redeed items in your own house!
        }
    }

    public void OnFlip(Mobile from)
    {
        if (ItemID == NorthID)
            ItemID = WestID;
        else if (ItemID == WestID)
            ItemID = NorthID;
    }
}

[SerializationGenerator(0, false)]
public partial class DragonLanternDeed : Item
{
    public override int LabelNumber => 1098392; // dragon lantern

    [Constructible]
    public DragonLanternDeed()
        : base(0x14F0)
    {
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        BaseHouse house = BaseHouse.FindHouseAt(from);

        if (house != null && house.IsCoOwner(from))
        {
            from.BeginTarget(-1, true, TargetFlags.None, new TargetStateCallback<object>(Placement_OnTarget), null);
        }
        else
        {
            from.SendLocalizedMessage(502092); // You must be in your house to do this.
        }
    }

    public void Placement_OnTarget(Mobile from, object targeted, object state)
    {
        IPoint3D p = targeted as IPoint3D;
        Map map = from.Map;

        if (p == null || map == null || Deleted)
            return;

        if (IsChildOf(from.Backpack))
        {
            Point3D loc = new Point3D(p);
            BaseHouse house = BaseHouse.FindHouseAt(loc, from.Map, 16);

            if (house != null && house.IsCoOwner(from))
            {
                Item addon = new DragonLantern();

                addon.MoveToWorld(loc, from.Map);

                house.Addons.Add(addon);
                Delete();
            }
            else
            {
                from.SendLocalizedMessage(1042036); // That location is not in your house.
            }
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }
}
