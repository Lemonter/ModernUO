using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Teleporter/ShipLadder.cs and
///     ShipSign.cs). The ladders up and down the sunken ship, and the labels beside them.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShipLadder : Item
{
    [SerializableField(0, setter: "private")]
    private Point3D _pointDest;

    [SerializableField(1, setter: "private")]
    private Map _map;

    [Constructible]
    public ShipLadder() : base(0x08A6) => Movable = false;

    [Constructible]
    public ShipLadder(Point3D point, Map map, int id) : base(0x08A6)
    {
        ItemID = id;
        _pointDest = point;
        _map = map;
        Movable = false;
    }

    public override void OnDoubleClickDead(Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            BaseCreature.TeleportPets(from, _pointDest, from.Map);
            from.MoveToWorld(_pointDest, _map);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.Alive)
        {
            return;
        }

        if (from.InRange(Location, 2))
        {
            BaseCreature.TeleportPets(from, _pointDest, from.Map);
            from.MoveToWorld(_pointDest, _map);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ShipSign : Item
{
    [SerializableField(0, setter: "private")]
    private int _cliloc;

    [Constructible]
    public ShipSign() : base(0xBD2) => Movable = false;

    [Constructible]
    public ShipSign(int id, int cliloc) : base(0xBD2)
    {
        ItemID = id;
        _cliloc = cliloc;
        Movable = false;
    }

    public override int LabelNumber => _cliloc;
}
