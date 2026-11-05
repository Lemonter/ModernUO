using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флага MondainsLegacy.PaintedCaves в репозитории нет — проверка
///     отключения подземелья опущена.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PaintedCavesTele : Teleporter
{
    [Constructible]
    public PaintedCavesTele() : base(new Point3D(6308, 892, 0), Map.Trammel)
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.NetState == null || !m.NetState.SupportsExpansion(Expansion.ML))
        {
            // You must upgrade to the Mondain's Legacy expansion in order to enter here.
            m.SendLocalizedMessage(1072608);
            return true;
        }

        return base.OnMoveOver(m);
    }
}
