using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флагов MondainsLegacy.Labyrinth в репозитории нет — проверка
///     отключения подземелья опущена.
/// </summary>
[SerializationGenerator(0, false)]
public partial class LabyrinthIslandTele : Item
{
    [Constructible]
    public LabyrinthIslandTele() : base(0x2FD4)
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            from.MoveToWorld(new Point3D(1731, 978, -80), Map);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class LabyrinthTele : Item
{
    [Constructible]
    public LabyrinthTele() : base(0x248B)
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.NetState == null || !from.NetState.SupportsExpansion(Expansion.ML))
        {
            // You must upgrade to the Mondain's Legacy expansion in order to enter here.
            from.SendLocalizedMessage(1072608);
            return;
        }

        if (from.InRange(Location, 2))
        {
            var p = new Point3D(330, 1973, 0);
            BaseCreature.TeleportPets(from, p, Map);
            from.MoveToWorld(p, Map);
        }
    }
}
