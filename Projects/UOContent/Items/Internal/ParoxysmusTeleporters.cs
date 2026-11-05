using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флага MondainsLegacy.PalaceOfParoxysmus в репозитории нет, типа
///     MagicalRope тоже — проверка отключения и требование верёвки опущены.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ParoxysmusTele : Teleporter
{
    [Constructible]
    public ParoxysmusTele() : base(new Point3D(6222, 335, 60), Map.Trammel)
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.NetState == null || !m.NetState.SupportsExpansion(Expansion.ML))
        {
            m.SendLocalizedMessage(1072608); // You must upgrade to the Mondain's Legacy expansion...
            return true;
        }

        var rope = m.Backpack?.FindItemByType<AcidProofRope>(true);

        if (rope == null)
        {
            // You have no way to lower yourself safely into the enormous sinkhole.
            m.SendLocalizedMessage(1074272);
            return true;
        }

        if (Utility.RandomDouble() < 0.3)
        {
            // Your rope is severely damaged by the acidic environment...
            m.SendLocalizedMessage(1075097);
            rope.Delete();
        }
        else
        {
            m.SendLocalizedMessage(1075098); // Your rope has been weakened by the acidic environment.
        }

        return base.OnMoveOver(m);
    }
}

[SerializationGenerator(0, false)]
public partial class ParoxysmusTeleFel : Teleporter
{
    [Constructible]
    public ParoxysmusTeleFel() : base(new Point3D(6222, 335, 60), Map.Felucca)
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.NetState == null || !m.NetState.SupportsExpansion(Expansion.ML))
        {
            m.SendLocalizedMessage(1072608);
            return true;
        }

        var rope = m.Backpack?.FindItemByType<AcidProofRope>(true);

        if (rope == null)
        {
            m.SendLocalizedMessage(1074272);
            return true;
        }

        if (Utility.RandomDouble() < 0.3)
        {
            m.SendLocalizedMessage(1075097);
            rope.Delete();
        }
        else
        {
            m.SendLocalizedMessage(1075098);
        }

        return base.OnMoveOver(m);
    }
}
