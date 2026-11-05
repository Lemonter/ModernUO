using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (WrongBedrolls.cs) — the bedrolls scattered through the
///     Wrong cell block. Tearing one apart either drops you into a tunnel (when the spawner gave it a
///     destination), springs a trap, or turns up a bar of soap.
///
///     Adapted: the two raw-network SendMessageTo helpers became LocalOverheadMessage/SendLocalizedMessage;
///     AOS.Damage takes the source mobile here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WrongBedrollBase : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _pointDest;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BedrollSpawner _bedrollSpawner;

    public WrongBedrollBase(int id) : base(id)
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        from.RevealingAction();

        if (_pointDest != Point3D.Zero)
        {
            var tunnel = new MysteriousTunnel();
            Effects.PlaySound(from.Location, from.Map, 0x3BD);

            tunnel.PointDest = _pointDest;
            tunnel.MoveToWorld(Location, Map);
            _bedrollSpawner?.MysteriousTunnels.Add(tunnel);
        }
        else if (Utility.RandomDouble() < 0.5)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502999); // You set off a trap!
            AOS.Damage(from, from, 40, 0, 100, 0, 0, 0);

            var loc = GetWorldLocation();
            var facet = Map;

            switch (Utility.Random(3))
            {
                case 0:
                    Effects.SendLocationEffect(loc, facet, 0x36BD, 15, 10);
                    Effects.PlaySound(loc, facet, 0x307);
                    break;
                case 1:
                    Effects.PlaySound(loc, facet, 0x307);
                    Effects.SendLocationEffect(new Point3D(loc.X - 1, loc.Y, loc.Z), facet, 0x36BD, 15);
                    Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y, loc.Z), facet, 0x36BD, 15);
                    Effects.SendLocationEffect(new Point3D(loc.X, loc.Y - 1, loc.Z), facet, 0x36BD, 15);
                    Effects.SendLocationEffect(new Point3D(loc.X, loc.Y + 1, loc.Z), facet, 0x36BD, 15);
                    Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y + 1, loc.Z + 11), facet, 0x36BD, 15);
                    break;
                case 2:
                    Effects.SendLocationEffect(loc, facet, 0x113A, 10, 20);
                    Effects.PlaySound(loc, facet, 0x231);
                    break;
            }
        }
        else if (Utility.RandomDouble() < 0.01)
        {
            var soap = new Soap();

            Effects.PlaySound(from.Location, from.Map, 0x247); // powder

            if (Utility.RandomBool())
            {
                from.AddToBackpack(soap);
                from.SendLocalizedMessage(1152268, "soap");
            }
            else
            {
                soap.MoveToWorld(Location, Map);
            }
        }
        else
        {
            Effects.PlaySound(from.Location, from.Map, 0x3E3); // leather
            from.SendLocalizedMessage(1152212); // You tear the bedroll to pieces but find nothing.
        }

        Delete();
    }
}

[SerializationGenerator(0, false)]
public partial class WrongBedrollSouth : WrongBedrollBase
{
    public override int LabelNumber => 1022645; // bedroll

    [Constructible]
    public WrongBedrollSouth() : base(0x0A56)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WrongBedrollEast : WrongBedrollBase
{
    public override int LabelNumber => 1022645; // bedroll

    [Constructible]
    public WrongBedrollEast() : base(0xA55)
    {
    }
}
