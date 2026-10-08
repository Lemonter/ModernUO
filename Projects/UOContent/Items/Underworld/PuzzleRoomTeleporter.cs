using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/PuzzleRoom/
// PuzzleRoomTeleporter.cs) — gates the Puzzle Room entrance behind carrying a MagicKey.
[SerializationGenerator(0, false)]
public partial class PuzzleRoomTeleporter : Teleporter
{
    [Constructible]
    public PuzzleRoomTeleporter()
    {
    }

    public override bool CanTeleport(Mobile m)
    {
        if (m.Backpack == null)
        {
            return false;
        }

        if (m.Backpack.FindItemByType(typeof(MagicKey)) == null)
        {
            m.SendLocalizedMessage(1113393); // You must carry a magic key to enter this room.
            return false;
        }

        return base.CanTeleport(m);
    }
}
