using System;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Blackthorn;

/// <summary>
///     Ported from ServUO's Blackthorn Revamped dungeon (Generate.cs) — `[GenBlackthorn` builds the
///     dungeon on both facets: the jail doors, the Felucca entrance teleporters, the row of blockers that
///     keep players out of the unfinished corridor, and the two spawn files (creatures and agents).
///
///     Deferred to its own port: ServUO's <c>DungeonHitchingPost</c> (a full stable-post implementation,
///     ~400 lines) that stands at the dungeon entrance. Everything else is placed as in the original.
/// </summary>
public static class BlackthornGenerator
{
    private static readonly Point3D[] Blockers =
    {
        new(6435, 2425, -5), new(6435, 2424, -5), new(6435, 2423, -5), new(6435, 2422, -5), new(6435, 2421, -5),
        new(6434, 2421, -5), new(6433, 2421, -5), new(6432, 2421, -5),
        new(6442, 2425, -5), new(6442, 2424, -5), new(6442, 2423, -5), new(6442, 2422, -5), new(6442, 2421, -5),
        new(6443, 2421, -5), new(6444, 2421, -5), new(6445, 2421, -5)
    };

    public static void Initialize()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    /// <summary>Blackthorn is part of the world: after every load the dungeon is checked and completed when
    /// its hitching post is missing. The post persists, so a normal boot finds it and does nothing.</summary>
    private static void OnServerStarted() => Timer.DelayCall(TimeSpan.FromSeconds(8), EnsureSetup);

    private static void EnsureSetup()
    {
        if (HasHitchingPost())
        {
            return;
        }

        try
        {
            GenerateOnCommand(null);
            _logger.Information("Blackthorn dungeon is set up.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Blackthorn dungeon setup failed");
        }
    }

    private static readonly Server.Logging.ILogger _logger =
        Server.Logging.LogFactory.GetLogger(typeof(BlackthornGenerator));

    private static bool HasHitchingPost()
    {
        foreach (var item in Map.Trammel.GetItemsInRange(new Point3D(6428, 2677, 0), 0))
        {
            if (item is DungeonHitchingPost)
            {
                return true;
            }
        }

        return false;
    }

    private static void GenerateOnCommand(CommandEventArgs e)
    {
        Build(Map.Trammel);
        Build(Map.Felucca);

        XmlSpawner.XmlLoadDefaults("BlackthornDungeonCreature.xml", e?.Mobile);
        XmlSpawner.XmlLoadDefaults("BlackthornDungeonAgent.xml", e?.Mobile);

        e?.Mobile.SendMessage(0x40, "Подземелье Блэкторна установлено.");
    }

    public static void Build(Map map)
    {
        var post = new DungeonHitchingPost();
        post.MoveToWorld(new Point3D(6428, 2677, 0), map);

        var door = new MetalDoor(DoorFacing.WestCCW);
        door.MoveToWorld(new Point3D(6409, 2695, 0), map);

        door = new MetalDoor(DoorFacing.EastCW);
        door.MoveToWorld(new Point3D(6410, 2695, 0), map);

        door = new MetalDoor(DoorFacing.WestCW);
        door.MoveToWorld(new Point3D(6409, 2664, 0), map);

        door = new MetalDoor(DoorFacing.EastCCW);
        door.MoveToWorld(new Point3D(6410, 2664, 0), map);

        door = new MetalDoor(DoorFacing.SouthCW);
        door.MoveToWorld(new Point3D(6394, 2680, 0), map);

        door = new MetalDoor(DoorFacing.NorthCCW);
        door.MoveToWorld(new Point3D(6394, 2679, 0), map);

        door = new MetalDoor(DoorFacing.NorthCW);
        door.MoveToWorld(new Point3D(6425, 2679, 0), map);

        door = new MetalDoor(DoorFacing.SouthCCW);
        door.MoveToWorld(new Point3D(6425, 2680, 0), map);

        // Trammel's side of this passage already exists in the world teleporters; only Felucca needs it.
        if (map == Map.Felucca)
        {
            for (var i = 0; i < 4; i++)
            {
                var toDungeon = new Teleporter(new Point3D(1517, 1417 + i, 9), map);
                toDungeon.MoveToWorld(new Point3D(6440, 2677 + i, 20), map);

                var fromDungeon = new Teleporter(new Point3D(6440, 2677 + i, 20), map);
                fromDungeon.MoveToWorld(new Point3D(1517, 1417 + i, i == 3 ? 12 : 9), map);
            }
        }

        foreach (var location in Blockers)
        {
            var blocker = new Blocker();
            blocker.MoveToWorld(location, map);
        }
    }
}
