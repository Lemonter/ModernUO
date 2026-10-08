using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Underworld/ExperimentalRoom/
// ExperimentalRoomDoor.cs) — a door gated on carrying an active ExperimentalGem whose
// CurrentRoom has progressed past the room this door guards (RoomZero always passes, it's
// the entrance from the lobby).
[SerializationGenerator(0, false)]
public partial class ExperimentalRoomDoor : MetalDoor2
{
    public override string DefaultName => "дверь";

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Room _room;

    [Constructible]
    public ExperimentalRoomDoor(Room room, DoorFacing facing) : base(facing) => _room = room;

    public override void Use(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            from.SendMessage("Ты открываешь дверь божественной силой.");
            base.Use(from);
            return;
        }

        var pack = from.Backpack;
        var hasGem = false;

        if (pack != null)
        {
            foreach (var item in pack.FindItemsByType(typeof(ExperimentalGem)))
            {
                hasGem = true;
                var gem = (ExperimentalGem)item;

                if (gem.Active && (gem.CurrentRoom > _room || _room == Room.RoomZero))
                {
                    base.Use(from);
                    return;
                }
            }

            if (!hasGem)
            {
                from.SendLocalizedMessage(1113410); // You must have an active Experimental Gem to enter that room.
            }
        }

        if (hasGem)
        {
            from.SendLocalizedMessage(1113411); // You have not yet earned access to that room!
        }
    }
}

// Ported alongside ExperimentalRoomDoor (same source file) — an invisible, immovable tile
// that blocks movement into a room unless the mover is carrying a sufficiently-progressed
// active ExperimentalGem. Placed directly on top of each ExperimentalRoomDoor tile so
// stepping around the door (e.g. via a mount or pet) doesn't bypass the gate.
[SerializationGenerator(0, false)]
public partial class ExperimentalRoomBlocker : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Room _room;

    [Constructible]
    public ExperimentalRoomBlocker(Room room) : base(7107)
    {
        _room = room;

        Visible = false;
        Movable = false;
    }

    public override bool OnMoveOver(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        var pack = from.Backpack;

        if (pack != null)
        {
            foreach (var item in pack.FindItemsByType(typeof(ExperimentalGem)))
            {
                var gem = (ExperimentalGem)item;

                if (gem.Active && (gem.CurrentRoom > _room || _room == Room.RoomZero))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
