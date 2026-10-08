using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
/// The way into a city house's cellar and back: double-click within a step of it and you are at
/// the other end — the hatch in the ground floor leads down, the ladder in the cellar up. Only the
/// household goes down; anyone caught in the cellar can still climb out. Pets come along.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonBasementHatch : Item
{
    // A ladder, as the mine shafts use.
    private const int Graphic = 0x79E;

    [SerializableField(0)]
    private MahaonCityHouse _house;

    [SerializableField(1)]
    private Point3D _destination;

    [Constructible]
    public MahaonBasementHatch(MahaonCityHouse house, Point3D destination, string name) : base(Graphic)
    {
        Movable = false;
        _house = house;
        _destination = destination;
        Name = name;
    }

    private bool LeadsDown => _destination.Z < Z;

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 1) || from.Map != Map)
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        if (LeadsDown && House?.IsFriend(from) != true)
        {
            from.SendMessage(0x22, "Люк заперт — в подвал пускают только своих.");
            return;
        }

        BaseCreature.TeleportPets(from, Destination, Map);
        from.MoveToWorld(Destination, Map);
        from.PlaySound(0x241);
    }
}
