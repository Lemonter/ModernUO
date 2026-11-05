using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Blackthorn Revamped dungeon (Items/DecorativeShardShields.cs) — the 27 shard
///     shield wall decorations and the deed that lets a house owner pick one.
///
///     Adapted: <c>IChopable</c> does not exist here, so the axe path is gone (the shield still re-deeds on
///     double-click); <c>ObjectPropertyList.Enabled</c> became <c>ForceShowProperties => true</c>; the
///     demolition confirmation gump is skipped — an owner in range gets the deed straight back.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DecorativeShardShield : Item, IAddon
{
    [Constructible]
    public DecorativeShardShield() : this(0x6380)
    {
    }

    [Constructible]
    public DecorativeShardShield(int itemID) : base(itemID)
    {
        Movable = false;
    }

    public override bool ForceShowProperties => true;

    public Item Deed => new DecorativeShardShieldDeed();

    public bool FacingEast => ItemID <= 0x639A;

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house == null || !house.IsOwner(from))
        {
            // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
            from.SendLocalizedMessage(1049784);
            return;
        }

        if (from.Backpack?.TryDropItem(from, Deed, false) != true)
        {
            from.SendLocalizedMessage(1074361); // The reward could not be given. Make sure you have room in your pack.
            return;
        }

        Delete();
    }

    public bool CouldFit(IPoint3D p, Map map)
    {
        if (map == null || !map.CanFit(p.X, p.Y, p.Z, ItemData.Height))
        {
            return false;
        }

        return FacingEast
            ? BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map) // west wall
            : BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map); // north wall
    }
}

[SerializationGenerator(0, false)]
public partial class DecorativeShardShieldDeed : Item
{
    public override int LabelNumber => 1153729; // Deed for a Decorative Shard Shield

    [Constructible]
    public DecorativeShardShieldDeed() : base(0x14F0)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public static int GetSouthItemID(int south) => south + 27;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
            return;
        }

        from.SendGump(new SelectionGump(this));
    }

    private sealed class SelectionGump : Gump
    {
        private const int Start = 25472;
        private const int End = 25498;

        private readonly DecorativeShardShieldDeed _deed;

        public SelectionGump(DecorativeShardShieldDeed deed) : base(150, 50)
        {
            _deed = deed;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(50, 89, 647, 505, 2600);
            AddLabel(103, 114, 0, @"Выберите вариант:");

            var itemID = Start;

            for (var i = 0; i < 27; i++)
            {
                int x;
                int y;

                if (i <= 8)
                {
                    x = 82 + i * 65;
                    y = 180;
                }
                else if (i < 18)
                {
                    x = 82 + (i - 9) * 65;
                    y = 330;
                }
                else
                {
                    x = 82 + (i - 18) * 65;
                    y = 480;
                }

                AddItem(x, y, itemID);
                AddTooltip(1104344 + i);
                AddButton(x + 10, y - 25, 0x845, 0x846, itemID);
                AddTooltip(1104344 + i);

                itemID++;
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_deed == null || _deed.Deleted)
            {
                return;
            }

            var id = info.ButtonID;

            if (id < Start || id > End)
            {
                return;
            }

            sender.Mobile.SendLocalizedMessage(1049780); // Where would you like to place this decoration?
            sender.Mobile.Target = new PlacementTarget(_deed, id);
        }
    }

    private sealed class PlacementTarget : Target
    {
        private readonly DecorativeShardShieldDeed _deed;
        private readonly int _itemID;

        public PlacementTarget(DecorativeShardShieldDeed deed, int itemID) : base(-1, true, TargetFlags.None)
        {
            _deed = deed;
            _itemID = itemID;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_deed == null || _deed.Deleted)
            {
                return;
            }

            if (!_deed.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
                return;
            }

            var house = BaseHouse.FindHouseAt(from);

            if (house == null || !house.IsOwner(from))
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
                return;
            }

            if (targeted is not IPoint3D point)
            {
                return;
            }

            var map = from.Map;

            if (map == null)
            {
                return;
            }

            var p = new Point3D(point);
            var data = TileData.ItemTable[_itemID & TileData.MaxItemValue];

            if (!map.CanFit(p, data.Height))
            {
                from.SendLocalizedMessage(500269); // You cannot build that there.
                return;
            }

            house = BaseHouse.FindHouseAt(p, map, data.Height);

            if (house == null || !house.IsCoOwner(from))
            {
                from.SendLocalizedMessage(1042036); // That location is not in your house.
                return;
            }

            var north = BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map);
            var west = BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map);

            if (north && west)
            {
                from.SendGump(new FacingGump(_deed, _itemID, p, house));
            }
            else if (north || west)
            {
                var shield = north
                    ? new DecorativeShardShield(GetSouthItemID(_itemID))
                    : new DecorativeShardShield(_itemID);

                house.Addons.Add(shield);
                shield.MoveToWorld(p, map);

                _deed.Delete();
            }
            else
            {
                from.SendLocalizedMessage(1049781); // This decoration must be placed next to a wall.
            }
        }
    }

    private sealed class FacingGump : Gump
    {
        private readonly DecorativeShardShieldDeed _deed;
        private readonly int _itemID;
        private readonly Point3D _location;
        private readonly BaseHouse _house;

        public FacingGump(DecorativeShardShieldDeed deed, int itemID, Point3D location, BaseHouse house) : base(150, 50)
        {
            _deed = deed;
            _itemID = itemID;
            _location = location;
            _house = house;

            AddBackground(0, 0, 300, 150, 0xA28);

            AddItem(90, 30, itemID);
            AddItem(180, 30, GetSouthItemID(itemID));

            AddButton(50, 35, 0x867, 0x869, 2); // east
            AddButton(145, 35, 0x867, 0x869, 1); // south
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_deed == null || _deed.Deleted || _house == null)
            {
                return;
            }

            DecorativeShardShield shield = info.ButtonID switch
            {
                2 => new DecorativeShardShield(_itemID),
                1 => new DecorativeShardShield(GetSouthItemID(_itemID)),
                _ => null
            };

            if (shield == null)
            {
                return;
            }

            _house.Addons.Add(shield);
            shield.MoveToWorld(_location, sender.Mobile.Map);

            _deed.Delete();
        }
    }
}
