using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/PuzzleRoom/
// MastermindPuzzleItem.cs) — the third Puzzle Room board, riding entirely on our own
// PuzzleChest (Engines/Khaldun/PuzzleChest.cs), which already ships the full cylinder-guess
// gump/hint/damage-on-fail machinery. Only the entry gate (must be standing in the Puzzle
// Room) and the unique success reward (an ExperimentalGem) are added here.
[SerializationGenerator(0, false)]
public partial class MastermindPuzzleItem : PuzzleChest
{
    [SerializableField(0)]
    private MagicKey _key;

    public override int LabelNumber => 1113379; // Puzzle Board

    [Constructible]
    public MastermindPuzzleItem(MagicKey key) : base(0x2AAA)
    {
        Hue = 914;
        _key = key;
        Movable = true;
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (MazePuzzleItem.IsInPuzzleRoom(from))
        {
            base.OnDoubleClick(from);
        }
    }

    public override void LockPick(Mobile from)
    {
        base.LockPick(from);

        _key?.Decay();

        // The Experimental Room this gem unlocks was ported later in the same migration
        // pass (see dev-docs/shadowguard-migration/AFFECTED-SCRIPTS.md) but this reward
        // was never restored to match — without it the room is unreachable by any normal
        // player (ExperimentalRoomDoor/Blocker/Region all gate entry on carrying one).
        var gem = new ExperimentalGem { Owner = from };

        if (from.Backpack?.TryDropItem(from, gem, false) != true)
        {
            gem.MoveToWorld(from.Location, from.Map);
        }

        from.SendLocalizedMessage(1113382); // You've solved the puzzle!! An item has been placed in your bag.
    }
}
