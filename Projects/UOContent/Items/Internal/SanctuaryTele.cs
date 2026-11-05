using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флага MondainsLegacy.Sanctuary в репозитории нет — проверка
///     отключения подземелья опущена.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SanctuaryTele : Teleporter
{
    [Constructible]
    public SanctuaryTele() : base(new Point3D(6172, 22, 0), Map.Trammel)
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.NetState == null || !m.NetState.SupportsExpansion(Expansion.ML))
        {
            m.SendLocalizedMessage(1072608); // You must upgrade to the Mondain's Legacy expansion...
            return true;
        }

        return base.OnMoveOver(m);
    }
}
