using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/UnderworldPuzzle.cs) — a "shift-the-colored-blocks" board puzzle, unrelated to
// ICircuitTrap; UnderworldPuzzleBox hands this out. Reward table simplified: 4 of the 13
// original reward types (VoidEssence, SilverSerpentVenom, ScouringToxin, ToxicVenomSac)
// don't exist anywhere in this codebase or ServUO's own shared reward files — replaced with
// the existing Mahaon Imbuing material (EnchantedEssence) rather than inventing new item
// classes for a single reward table. LuckyCoin is back in the table, at the original's 2-6
// stack, now that the coin and the Fountain of Fortune it feeds are both in place. The
// other 8 (MouldingBoard..FlouredBreadBoard, see MazeRewards.cs) are real, unchanged ports.
[SerializationGenerator(0, false)]
public partial class UnderworldPuzzleItem : BaseDecayingItem
{
    [SerializableField(0)]
    private UnderworldPuzzleSolution _solution;

    [SerializableField(1)]
    private UnderworldPuzzleSolution _currentSolution;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _attempts;

    public override int LabelNumber => 1113379; // Puzzle Board
    public override int Lifespan => 1800;
    public override bool UseSeconds => false;

    [Constructible]
    public UnderworldPuzzleItem() : base(0x2AAA)
    {
        LootType = LootType.Blessed;
        Weight = 5.0;
        Hue = 0x281;
        _attempts = 0;

        _solution = new UnderworldPuzzleSolution(this);
        _currentSolution = new UnderworldPuzzleSolution(this, _solution.Index);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.CloseGump<UnderworldPuzzleGump>();
        from.SendGump(new UnderworldPuzzleGump(from, this));
    }

    private static readonly Type[] RewardTypes =
    {
        typeof(MouldingBoard), typeof(DoughBowl), typeof(HornedTotemPole),
        typeof(LargeSquarePillow), typeof(LargeDiamondPillow), typeof(DustyPillow),
        typeof(StatuePedestal), typeof(FlouredBreadBoard), typeof(LuckyCoin)
    };

    public bool SubmitSolution(Mobile m, UnderworldPuzzleSolution solution)
    {
        if (m == null)
        {
            return false;
        }

        if (!solution.Matches(_solution))
        {
            m.SendLocalizedMessage(1150177); // Incorrect Code Sequence. Access Denied.
            return false;
        }

        var reward = Utility.RandomDouble() < 0.35
            ? new EnchantedEssence(Utility.RandomMinMax(2, 5))
            : Loot.Construct(RewardTypes[Utility.Random(RewardTypes.Length)]);

        if (reward != null)
        {
            if (reward is LuckyCoin)
            {
                reward.Amount = Utility.RandomMinMax(2, 6);
            }

            if (m.Backpack == null || !m.Backpack.TryDropItem(m, reward, false))
            {
                m.BankBox?.DropItem(reward);
            }
        }

        m.PlaySound(0x3D);
        m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1113579, m.NetState); // Correct Code Entered. Crystal Lock Disengaged.

        Delete();
        return true;
    }

    public override void OnDelete()
    {
        base.OnDelete();

        if (RootParent is Mobile m)
        {
            m.CloseGump<UnderworldPuzzleGump>();
        }
    }
}

public enum PuzzlePiece
{
    None,
    RedSingle,
    RedDouble,
    RedTriple,
    RedBar,
    BlueSingle,
    BlueDouble,
    BlueTriple,
    BlueBar,
    GreenSingle,
    GreenDouble,
    GreenTriple,
    GreenBar
}

public enum PuzzleColor
{
    Red,
    Blue,
    Green
}

// A small self-contained POCO (not an Item/Mobile) — made codegen-serializable the same way
// PuzzleChestSolution already is in this codebase (Engines/Khaldun/PuzzleChest.cs), since it
// hangs off two [SerializableField] slots on UnderworldPuzzleItem above.
[SerializationGenerator(0)]
public partial class UnderworldPuzzleSolution
{
    public const int Length = 4;

    [DirtyTrackingEntity]
    private UnderworldPuzzleItem _item;

    [SerializableField(0)]
    private PuzzlePiece[] _rows;

    [SerializableField(1)]
    private int _index;

    [SerializableField(2)]
    private int _maxAttempts;

    public PuzzlePiece First { get => _rows[0]; set => _rows[0] = value; }
    public PuzzlePiece Second { get => _rows[1]; set => _rows[1] = value; }
    public PuzzlePiece Third { get => _rows[2]; set => _rows[2] = value; }
    public PuzzlePiece Fourth { get => _rows[3]; set => _rows[3] = value; }

    // Declared first: the generator picks the first matching constructor, and a deserialized
    // solution must know its item to mark it dirty.
    public UnderworldPuzzleSolution(UnderworldPuzzleItem item) : this() => _item = item;

    public UnderworldPuzzleSolution(UnderworldPuzzleItem item, int index) : this(index) => _item = item;

    public UnderworldPuzzleSolution()
    {
        _rows = new PuzzlePiece[Length];
        PickRandom();
    }

    public UnderworldPuzzleSolution(int index)
    {
        _rows = new PuzzlePiece[Length];
        LoadStartSolution(index);
    }

    public bool Matches(UnderworldPuzzleSolution check) => GetMatches(check) >= 4;

    public int GetMatches(UnderworldPuzzleSolution check)
    {
        var matches = 0;

        for (var i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] == check._rows[i])
            {
                matches++;
            }
        }

        return matches;
    }

    public void PickRandom()
    {
        _index = Utility.Random(16);

        (First, Second, Third, Fourth, _maxAttempts) = _index switch
        {
            0  => (PuzzlePiece.RedSingle, PuzzlePiece.BlueSingle, PuzzlePiece.RedSingle, PuzzlePiece.GreenSingle, 6),
            1  => (PuzzlePiece.GreenDouble, PuzzlePiece.RedBar, PuzzlePiece.None, PuzzlePiece.BlueTriple, 6),
            2  => (PuzzlePiece.None, PuzzlePiece.None, PuzzlePiece.RedBar, PuzzlePiece.RedTriple, 4),
            3  => (PuzzlePiece.BlueDouble, PuzzlePiece.None, PuzzlePiece.GreenDouble, PuzzlePiece.GreenDouble, 7),
            4  => (PuzzlePiece.BlueSingle, PuzzlePiece.GreenSingle, PuzzlePiece.GreenDouble, PuzzlePiece.RedBar, 7),
            5  => (PuzzlePiece.GreenDouble, PuzzlePiece.BlueBar, PuzzlePiece.RedSingle, PuzzlePiece.BlueSingle, 8),
            6  => (PuzzlePiece.GreenSingle, PuzzlePiece.RedSingle, PuzzlePiece.BlueDouble, PuzzlePiece.GreenBar, 5),
            7  => (PuzzlePiece.BlueDouble, PuzzlePiece.None, PuzzlePiece.BlueTriple, PuzzlePiece.None, 4),
            8  => (PuzzlePiece.GreenSingle, PuzzlePiece.GreenBar, PuzzlePiece.RedDouble, PuzzlePiece.RedSingle, 7),
            9  => (PuzzlePiece.BlueSingle, PuzzlePiece.GreenDouble, PuzzlePiece.None, PuzzlePiece.GreenTriple, 6),
            10 => (PuzzlePiece.BlueSingle, PuzzlePiece.RedSingle, PuzzlePiece.RedTriple, PuzzlePiece.None, 6),
            11 => (PuzzlePiece.GreenSingle, PuzzlePiece.None, PuzzlePiece.GreenTriple, PuzzlePiece.GreenDouble, 5),
            12 => (PuzzlePiece.RedTriple, PuzzlePiece.None, PuzzlePiece.None, PuzzlePiece.RedTriple, 6),
            13 => (PuzzlePiece.None, PuzzlePiece.BlueTriple, PuzzlePiece.GreenSingle, PuzzlePiece.BlueDouble, 7),
            14 => (PuzzlePiece.BlueTriple, PuzzlePiece.None, PuzzlePiece.BlueTriple, PuzzlePiece.None, 6),
            _  => (PuzzlePiece.RedSingle, PuzzlePiece.BlueDouble, PuzzlePiece.RedDouble, PuzzlePiece.GreenSingle, 6)
        };
    }

    public void LoadStartSolution(int index)
    {
        _index = index;

        (First, Second, Third, Fourth) = index switch
        {
            0  => (PuzzlePiece.RedSingle, PuzzlePiece.RedSingle, PuzzlePiece.RedSingle, PuzzlePiece.RedSingle),
            1  => (PuzzlePiece.BlueBar, PuzzlePiece.RedDouble, PuzzlePiece.RedBar, PuzzlePiece.BlueSingle),
            2  => (PuzzlePiece.GreenBar, PuzzlePiece.BlueDouble, PuzzlePiece.RedTriple, PuzzlePiece.RedBar),
            3  => (PuzzlePiece.RedSingle, PuzzlePiece.GreenDouble, PuzzlePiece.BlueTriple, PuzzlePiece.RedBar),
            4  => (PuzzlePiece.GreenSingle, PuzzlePiece.RedBar, PuzzlePiece.RedDouble, PuzzlePiece.GreenSingle),
            5  => (PuzzlePiece.RedBar, PuzzlePiece.GreenBar, PuzzlePiece.RedBar, PuzzlePiece.BlueBar),
            6  => (PuzzlePiece.RedBar, PuzzlePiece.GreenDouble, PuzzlePiece.GreenDouble, PuzzlePiece.RedBar),
            7  => (PuzzlePiece.GreenBar, PuzzlePiece.BlueSingle, PuzzlePiece.BlueTriple, PuzzlePiece.RedSingle),
            8  => (PuzzlePiece.GreenBar, PuzzlePiece.None, PuzzlePiece.GreenTriple, PuzzlePiece.GreenSingle),
            9  => (PuzzlePiece.RedBar, PuzzlePiece.RedBar, PuzzlePiece.RedBar, PuzzlePiece.RedBar),
            10 => (PuzzlePiece.RedSingle, PuzzlePiece.None, PuzzlePiece.BlueDouble, PuzzlePiece.RedBar),
            11 => (PuzzlePiece.RedDouble, PuzzlePiece.GreenDouble, PuzzlePiece.RedDouble, PuzzlePiece.BlueDouble),
            12 => (PuzzlePiece.GreenTriple, PuzzlePiece.BlueBar, PuzzlePiece.GreenTriple, PuzzlePiece.RedBar),
            13 => (PuzzlePiece.RedSingle, PuzzlePiece.GreenBar, PuzzlePiece.GreenBar, PuzzlePiece.RedSingle),
            14 => (PuzzlePiece.GreenTriple, PuzzlePiece.GreenBar, PuzzlePiece.BlueBar, PuzzlePiece.GreenSingle),
            _  => (PuzzlePiece.BlueTriple, PuzzlePiece.GreenDouble, PuzzlePiece.BlueSingle, PuzzlePiece.RedDouble)
        };
    }
}
