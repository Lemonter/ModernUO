using ModernUO.Serialization;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     Myrmidex pit entrances, ported from ServUO (Scripts/Quests/Eodon/MyrmidexThreat/Items/Teleporter.cs).
///
///     Dropped: the <c>MyrmidexInvasionSystem.IsAlliedWith</c> gate on <see cref="MyrmidexPitTeleporter" /> — this
///     shard has no allegiance system, so the pits are open and the teleporter is a plain <see cref="Teleporter" />
///     subclass kept for content identity.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MyrmidexPitTeleporter : Teleporter
{
    [Constructible]
    public MyrmidexPitTeleporter()
    {
    }

    [Constructible]
    public MyrmidexPitTeleporter(Point3D dest, Map map) : base(dest, map)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MyrmidexQueenTeleporter : Teleporter
{
    [Constructible]
    public MyrmidexQueenTeleporter()
    {
    }

    [Constructible]
    public MyrmidexQueenTeleporter(Point3D dest, Map map) : base(dest, map)
    {
    }

    public override bool OnMoveOver(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return base.OnMoveOver(from);
        }

        if (from is PlayerMobile pm &&
            MLQuestSystem.GetContext(pm)?.IsDoingQuest(typeof(InsecticideAndRegicideQuest)) == true)
        {
            return base.OnMoveOver(from);
        }

        // You must be on the "Insecticide & Regicide" quest to enter.
        from.SendLocalizedMessage(1156840);

        return false;
    }
}
