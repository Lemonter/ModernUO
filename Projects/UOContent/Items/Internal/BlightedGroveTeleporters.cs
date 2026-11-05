using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флагов MondainsLegacy.BlightedGrove в репозитории нет — проверка
///     отключения подземелья опущена.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlightedGroveTele : Teleporter
{
    [Constructible]
    public BlightedGroveTele()
    {
    }

    public static BoneMachete GetBoneMachete(Mobile m)
    {
        for (var i = 0; i < m.Items.Count; i++)
        {
            if (m.Items[i] is BoneMachete machete)
            {
                return machete;
            }
        }

        return m.Backpack?.FindItemByType<BoneMachete>(true);
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.NetState == null || !m.NetState.SupportsExpansion(Expansion.ML))
        {
            m.SendLocalizedMessage(1072608); // You must upgrade to the Mondain's Legacy expansion...
            return true;
        }

        var machete = GetBoneMachete(m);

        if (machete == null)
        {
            // You are unable to push your way through the tangling roots of the mighty tree.
            m.SendLocalizedMessage(1074275);
            return true;
        }

        if (0.6 > Utility.RandomDouble())
        {
            // Your bone handled machete has grown dull but you still manage to force your way past...
            m.SendLocalizedMessage(1075008);
        }
        else
        {
            machete.Delete();
            // Your bone handled machete snaps in half as you force your way through...
            m.SendLocalizedMessage(1075007);
        }

        return base.OnMoveOver(m);
    }
}

[SerializationGenerator(0, false)]
public partial class BlightedGroveTreeInTele : Teleporter
{
    [Constructible]
    public BlightedGroveTreeInTele()
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        m.SendLocalizedMessage(1074162); // You notice a hole in the tree and climb down
        return base.OnMoveOver(m);
    }
}

[SerializationGenerator(0, false)]
public partial class BlightedGroveTreeOutTele : Teleporter
{
    [Constructible]
    public BlightedGroveTreeOutTele()
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        m.SendLocalizedMessage(1074163); // You find a way to climb back outside the tree
        return base.OnMoveOver(m);
    }
}
