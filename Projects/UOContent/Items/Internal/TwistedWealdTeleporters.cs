using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Отброшено: флаг MondainsLegacy.TwistedWeald и проверка квеста
///     DreadhornQuest (в репозитории он закомментирован до появления Dreadhorn).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TwistedWealdTele : Teleporter
{
    [Constructible]
    public TwistedWealdTele() : base(new Point3D(2189, 1253, 0), Map.Ilshenar)
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
