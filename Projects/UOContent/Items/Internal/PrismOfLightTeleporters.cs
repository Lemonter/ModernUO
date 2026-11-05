using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флага MondainsLegacy.PrismOfLight и типа
///     PrismOfLightAdmissionTicket в репозитории нет — проверки опущены.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PrismOfLightTele : Teleporter
{
    [Constructible]
    public PrismOfLightTele() : base(new Point3D(6474, 188, 0), Map.Trammel)
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

[SerializationGenerator(0, false)]
public partial class PrismOfLightTeleFel : Teleporter
{
    [Constructible]
    public PrismOfLightTeleFel() : base(new Point3D(6474, 188, 0), Map.Felucca)
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.NetState == null || !m.NetState.SupportsExpansion(Expansion.ML))
        {
            m.SendLocalizedMessage(1072608);
            return true;
        }

        return base.OnMoveOver(m);
    }
}
