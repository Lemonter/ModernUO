using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Engines.Despise;
using Server.Mobiles;

namespace Server.Items;

/// <summary>Ported from ServUO's Despise Revamped dungeon (Scripts/Items/Internal/
/// DespiseTeleporter.cs) — a Teleporter that also drags along any pets/summons following
/// their owner (vanilla Teleporter doesn't), and refuses to carry a possessed DespiseCreature
/// through (those stay leashed to their WispOrb's own anchor logic instead).</summary>
[SerializationGenerator(0, false)]
public partial class DespiseTeleporter : Teleporter
{
    [Constructible]
    public DespiseTeleporter()
    {
    }

    public override bool CanTeleport(Mobile m) => m is not DespiseCreature && base.CanTeleport(m);

    public override void DoTeleport(Mobile m)
    {
        var map = MapDest;

        if (map == null || map == Map.Internal)
        {
            map = m.Map;
        }

        var p = PointDest;

        if (p == Point3D.Zero)
        {
            p = m.Location;
        }

        TeleportPets(m, p, map);

        var sendEffect = !m.Hidden || m.AccessLevel == AccessLevel.Player;

        if (SourceEffect && sendEffect)
        {
            Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);
        }

        m.MoveToWorld(p, map);

        if (DestEffect && sendEffect)
        {
            Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);
        }

        if (SoundID > 0 && sendEffect)
        {
            Effects.PlaySound(m.Location, m.Map, SoundID);
        }
    }

    public static void TeleportPets(Mobile master, Point3D loc, Map map)
    {
        var move = new List<Mobile>();

        foreach (var m in master.Map.GetMobilesInRange(master.Location, 3))
        {
            if (m is BaseCreature { Controlled: true } pet and not DespiseCreature && pet.ControlMaster == master)
            {
                if (pet.ControlOrder is OrderType.Guard or OrderType.Follow or OrderType.Come)
                {
                    move.Add(pet);
                }
            }
        }

        foreach (var m in move)
        {
            m.MoveToWorld(loc, map);
        }
    }
}

/// <summary>Ported from ServUO's Despise Revamped dungeon (same file as DespiseTeleporter) —
/// a static one-way gate rendered as 8 invisible teleporter tiles surrounding a visible
/// gate graphic, so stepping up to it from any direction triggers the teleport. Used for the
/// dungeon's internal gates between the start/good/evil/lower regions (see
/// DespiseRevampedSetup.SetupTeleporters).</summary>
[SerializationGenerator(0, false)]
public partial class GateTeleporter : Item
{
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D Destination
    {
        get => _destination;
        set
        {
            if (_destination == value)
            {
                return;
            }

            _destination = value;
            this.MarkDirty();
            AssignDestination(value);
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public Map DestinationMap
    {
        get => _destinationMap;
        set
        {
            if (_destinationMap == value)
            {
                return;
            }

            _destinationMap = value;
            this.MarkDirty();
            AssignMap(value);
        }
    }

    [SerializableField(2)]
    private List<InternalTeleporter> _teleporters;

    [Constructible]
    public GateTeleporter() : this(19343, 0, Point3D.Zero, null)
    {
    }

    public GateTeleporter(int id, int hue, Point3D destination, Map destinationMap) : base(id)
    {
        Hue = hue;
        Movable = false;

        _destination = destination;
        _destinationMap = destinationMap;

        AssignTeleporters();
    }

    private void AssignTeleporters()
    {
        if (_teleporters != null)
        {
            foreach (var tele in _teleporters.Where(t => t is { Deleted: false }))
            {
                tele.Delete();
            }
        }

        _teleporters = new List<InternalTeleporter>();

        for (var i = 0; i <= 7; i++)
        {
            var offset = (Direction)i;
            var tele = new InternalTeleporter(this, _destination, _destinationMap);

            var x = X;
            var y = Y;
            var z = Z;

            Movement.Movement.Offset(offset, ref x, ref y);
            tele.MoveToWorld(new Point3D(x, y, z), Map);

            _teleporters.Add(tele);
        }
    }

    public void AssignDestination(Point3D p)
    {
        if (_teleporters == null)
        {
            AssignTeleporters();
        }
        else
        {
            _teleporters.ForEach(t => t.PointDest = p);
        }
    }

    public void AssignMap(Map map)
    {
        if (_teleporters == null)
        {
            AssignTeleporters();
        }
        else
        {
            _teleporters.ForEach(t => t.MapDest = map);
        }
    }

    public override void OnMapChange()
    {
        _teleporters?.ForEach(t => t.Map = Map);
    }

    public override void OnLocationChange(Point3D old)
    {
        _teleporters?.ForEach(t => t.Location = new Point3D(X + (t.X - old.X), Y + (t.Y - old.Y), Z + (t.Z - old.Z)));
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _teleporters?.ForEach(t => t.Delete());
    }

    [SerializationGenerator(0, false)]
    public partial class InternalTeleporter : Teleporter
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private GateTeleporter _master;

        public InternalTeleporter(GateTeleporter master, Point3D dest, Map destMap) : base(dest, destMap, true)
        {
            _master = master;
        }

        public override bool OnMoveOver(Mobile m) => true;

        public override bool HandlesOnMovement => _master != null && Utility.InRange(_master.Location, Location, 1) && Map == _master.Map;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (_master == null || _master.Destination == Point3D.Zero || _master.Map == null || _master.Map == Map.Internal)
            {
                return;
            }

            if (m.Location != Location)
            {
                return;
            }

            foreach (var item in Map.GetItemsInRange(oldLocation, 0))
            {
                if (item is InternalTeleporter || item == _master)
                {
                    return;
                }
            }

            base.OnMoveOver(m);
        }
    }
}
