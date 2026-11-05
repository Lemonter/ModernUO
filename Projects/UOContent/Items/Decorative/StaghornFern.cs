using System;
using ModernUO.Serialization;
using Server.Multis;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StaghornFernAddon : Item
{
    [Constructible]
    public StaghornFernAddon() : base(0x9965)
    {
        Movable = false;
        Weight = 0.0;
    }

    public override int LabelNumber => 1154460; // Staghorn Fern

    public Item Deed => new StaghornFernDeed();

    [AfterDeserialization]
    private void AfterDeserialization() => Timer.DelayCall(TimeSpan.Zero, FixMovingCrate);

    public override void OnDoubleClick(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house == null || !house.IsCoOwner(from))
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        from.AddToBackpack(Deed);
        Delete();
    }

    private void FixMovingCrate()
    {
        if (Deleted || !(Movable || IsLockedDown))
        {
            return;
        }

        var deed = Deed;

        if (Parent is Item parent)
        {
            parent.AddItem(deed);
            deed.Location = Location;
        }
        else
        {
            deed.MoveToWorld(Location, Map);
        }

        Delete();
    }
}

[Flippable(0x14F0, 0x14EF)]
[SerializationGenerator(0, false)]
public partial class StaghornFernDeed : Item
{
    [Constructible]
    public StaghornFernDeed() : base(0x14F0) => LootType = LootType.Blessed;

    public override int LabelNumber => 1154460; // Staghorn Fern

    public override void OnDoubleClick(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(from);

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
        if (targeted is not IPoint3D p)
        {
            return;
        }

        var loc = new Point3D(p);

        var house = BaseHouse.FindHouseAt(loc, from.Map, 16);

        if (house == null || !house.IsCoOwner(from))
        {
            from.SendLocalizedMessage(1042036); // That location is not in your house.
            return;
        }

        var northWall = BaseAddon.IsWall(loc.X, loc.Y - 1, loc.Z, from.Map);
        var westWall = BaseAddon.IsWall(loc.X - 1, loc.Y, loc.Z, from.Map);

        if (northWall && westWall)
        {
            switch (from.Direction & Direction.Mask)
            {
                case Direction.North:
                case Direction.South:
                    {
                        northWall = true;
                        westWall = false;
                        break;
                    }
                case Direction.East:
                case Direction.West:
                    {
                        northWall = false;
                        westWall = true;
                        break;
                    }
                default:
                    {
                        from.SendMessage("Повернитесь лицом к стене, на которую хотите повесить трофей.");
                        return;
                    }
            }
        }

        var itemID = 0;

        if (northWall)
        {
            itemID = 0x9964;
        }
        else if (westWall)
        {
            itemID = 0x9965;
        }
        else
        {
            from.SendLocalizedMessage(1042626); // The trophy must be placed next to a wall.
        }

        if (itemID > 0)
        {
            Item addon = new StaghornFernAddon { ItemID = itemID };

            addon.MoveToWorld(loc, from.Map);

            house.Addons.Add(addon);
            Delete();
        }
    }
}
