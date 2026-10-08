using ModernUO.Serialization;
using Server.Regions;

namespace Server.Items;

// World-placement controller for the Underworld Maze of Death, Puzzle Room and Experimental
// Room, plus the standalone FountainOfFortune addon, mirroring ShadowguardController's
// pattern (per the shard owner's explicit call: no separate GM command, fixture placement
// happens once in the constructor like any other [Constructible] world fixture). Coordinates
// are the real OSI/ServUO ones from Scripts/Services/Underworld/Generate.cs.
//
// Real upstream bug fixed while placing the Experimental Room doors: Generate.cs's "Room 3
// to 4" door pair (the one gating the final room) is constructed with `Room.RoomTwo` instead
// of `Room.RoomThree` — the same room value already used for the "Room 2 to 3" pair right
// above it — while its paired ExperimentalRoomBlocker correctly uses `Room.RoomThree`. Left
// as-is, that door would unlock as soon as Room Two is cleared, letting a player skip Room
// Three's puzzle entirely (the blocker tile would still stop them, but the door itself
// wouldn't gate anything). Corrected to `Room.RoomThree` to match the blocker and the
// doors' own progressive-gating design.
//
// Not ported from Generate.cs: its "reveal tile" grid near a different TerMur coordinate
// range and the militia cannon/cannoneer placement — both look like unrelated world
// decoration for something else entirely (see dev-docs/shadowguard-migration/
// AFFECTED-SCRIPTS.md). The two locked MetalDoor2 fixtures Generate.cs places right on top
// of the Puzzle Room teleporter pair are also dropped — they duplicate the
// PuzzleRoomTeleporter's own MagicKey gate and Generate.cs itself has a copy-paste bug there
// (WeakEntityCollection.Add("sa", door) instead of door2), suggesting they were leftover/dead
// weight rather than a real second path in.
[SerializationGenerator(0, false)]
public partial class UnderworldPuzzleController : Item
{
    [Constructible]
    public UnderworldPuzzleController() : base(0x1869)
    {
        Movable = false;
        Visible = false;
        Name = "Underworld Puzzle Controller (Maze of Death / Puzzle Room / Experimental Room)";

        MazeOfDeathRegion.Initialize();
        ExperimentalRoomRegion.Initialize();
        PlaceFixtures();
    }

    private static void PlaceFixtures()
    {
        var map = Map.TerMur;

        // -- Puzzle Room ------------------------------------------------------------

        Place(new PuzzleBox(PuzzleType.WestBox), 1090, 1171, 11, map);
        Place(new PuzzleBox(PuzzleType.EastBox), 1104, 1171, 11, map);
        Place(new PuzzleBox(PuzzleType.NorthBox), 1097, 1163, 11, map);
        Place(new PuzzleBook(), 1109, 1153, -17, map);

        Place(
            new PuzzleRoomTeleporter { PointDest = new Point3D(1097, 1173, 1), MapDest = map },
            1097, 1175, 0, map
        );
        Place(
            new PuzzleRoomTeleporter { PointDest = new Point3D(1098, 1173, 1), MapDest = map },
            1098, 1175, 0, map
        );

        Place(new Teleporter { PointDest = new Point3D(1097, 1175, 0), MapDest = map }, 1097, 1173, 1, map);
        Place(new Teleporter { PointDest = new Point3D(1098, 1175, 0), MapDest = map }, 1098, 1173, 1, map);

        // -- Maze of Death ------------------------------------------------------------

        Place(new UnderworldPuzzleBox(), 1068, 1026, -37, map);
        Place(new GoldenCompass(), 1070, 1055, -34, map);
        Place(new RolledMapOfTheUnderworld { Movable = false }, 1072, 1055, -36, map);

        // -- Experimental Room --------------------------------------------------------

        Place(new ExperimentalRoomController(), 980, 1117, -42, map);

        PlaceDoorPair(Room.RoomZero, 984, 985, 1116, map);
        PlaceDoorPair(Room.RoomOne, 984, 985, 1102, map);
        PlaceDoorPair(Room.RoomTwo, 984, 985, 1090, map);
        PlaceDoorPair(Room.RoomThree, 984, 985, 1072, map); // fixed from Generate.cs's RoomTwo, see header comment

        Place(new ExperimentalRoomChest(), 984, 1064, -37, map);
        Place(new ExperimentalBook { Movable = false }, 995, 1114, -36, map);

        Place(new SecretDungeonDoor(DoorFacing.NorthCCW) { ClosedId = 87, OpenedId = 88, ItemID = 87 }, 1007, 1119, -42, map);
        Place(new LocalizedSign(3026, 1113407) { Movable = false }, 980, 1119, -37, map); // Experimental Room Access

        // -- Fountain of Fortune -------------------------------------------------------

        Place(new FountainOfFortune(), 1121, 957, -42, map);
    }

    private static void PlaceDoorPair(Room room, int westX, int eastX, int y, Map map)
    {
        Place(new ExperimentalRoomDoor(room, DoorFacing.WestCCW) { Hue = 1109 }, westX, y, -42, map);
        Place(new ExperimentalRoomBlocker(room), westX, y, -42, map);

        Place(new ExperimentalRoomDoor(room, DoorFacing.EastCW) { Hue = 1109 }, eastX, y, -42, map);
        Place(new ExperimentalRoomBlocker(room), eastX, y, -42, map);
    }

    private static void Place(Item item, int x, int y, int z, Map map) => item.MoveToWorld(new Point3D(x, y, z), map);
}
