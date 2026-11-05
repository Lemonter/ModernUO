using ModernUO.Serialization;
using Server.Regions;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Списка BaseHouse.Carpets в этом репозитории нет, поэтому ковёр
///     только переключает Movable и не ведёт учёт в доме.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DecorativeCarpet : Item, IDyable
{
    [Constructible]
    public DecorativeCarpet(int itemId) : base(itemId)
    {
        Weight = 1.0;
    }

    public override int Hue => Movable && IsInsideHouse() ? 253 : base.Hue;

    public override bool DisplayWeight => Movable;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Movable && IsInsideHouse())
        {
            list.Add(1113267); // (Double Click to Lockdown)
        }
    }

    public override bool OnDroppedToWorld(Mobile from, Point3D p)
    {
        if (!base.OnDroppedToWorld(from, p))
        {
            return false;
        }

        InvalidateProperties();
        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Region.Find(Location, Map) is not HouseRegion region)
        {
            return;
        }

        var house = region.House;

        if (house.IsOwner(from) || house.IsCoOwner(from))
        {
            Movable = !Movable;
            InvalidateProperties();
        }
        else
        {
            // Только владелец дома может закрепить ковёр.
            from.SendLocalizedMessage(1113268);
        }
    }

    public bool IsInsideHouse() => Region.Find(Location, Map).IsPartOf<HouseRegion>();

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }
}
