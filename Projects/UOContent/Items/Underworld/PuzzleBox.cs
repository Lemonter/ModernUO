using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/PuzzleRoom/
// PuzzleBox.cs) — the three "give me a puzzle board" stones in the Puzzle Room.
public enum PuzzleType
{
    WestBox,  // Maze #1
    EastBox,  // Maze #2
    NorthBox  // Mastermind
}

[SerializationGenerator(0, false)]
public partial class PuzzleBox : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PuzzleType _puzzleType;

    public override int LabelNumber => 1113486; // a puzzle box

    [Constructible]
    public PuzzleBox(PuzzleType type) : base(2472)
    {
        _puzzleType = type;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 3))
        {
            from.SendLocalizedMessage(3000268); // that is too far away.
            return;
        }

        if (from.Backpack == null)
        {
            return;
        }

        var key = from.Backpack.FindItemByType(typeof(MagicKey));
        var puzzleItem1 = from.Backpack.FindItemByType(typeof(MazePuzzleItem));
        var puzzleItem2 = from.Backpack.FindItemByType(typeof(MastermindPuzzleItem));

        if (key == null)
        {
            var x = Utility.RandomMinMax(1095, 1099);
            var y = Utility.RandomMinMax(1178, 1179);
            const int z = -1;

            var loc = from.Location;
            var p = new Point3D(x, y, z);
            BaseCreature.TeleportPets(from, p, Map.TerMur);
            from.MoveToWorld(p, Map.TerMur);

            from.PlaySound(0x1FE);
            Effects.SendLocationParticles(EffectItem.Create(loc, from.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
            Effects.SendLocationParticles(EffectItem.Create(p, from.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);
        }

        if (puzzleItem1 != null || puzzleItem2 != null)
        {
            from.SendMessage("Головоломка у тебя уже есть.");
            return;
        }

        var needed = _puzzleType == PuzzleType.NorthBox ? typeof(GoldPuzzleKey) : typeof(MagicKey);
        var item = from.Backpack.FindItemByType(needed);

        Item puzzle = null;

        if (item != null && key is MagicKey magicKey)
        {
            puzzle = _puzzleType == PuzzleType.NorthBox
                ? new MastermindPuzzleItem(magicKey)
                : new MazePuzzleItem(magicKey);
        }

        if (puzzle != null)
        {
            if (!from.Backpack.TryDropItem(from, puzzle, true))
            {
                puzzle.Delete();
            }
            else
            {
                from.SendMessage("Ты получаешь головоломку.");
            }
        }
        else
        {
            from.SendMessage("Нет нужного ключа, чтобы взять эту головоломку.");
        }
    }
}
