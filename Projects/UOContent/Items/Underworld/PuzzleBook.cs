using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/PuzzleRoom/
// PuzzleBook.cs) — the instructions book standing at the Puzzle Room entrance.
[SerializationGenerator(0, false)]
public partial class PuzzleBook : BrownBook
{
    public static readonly BookContent Content = new(
        "Instructions", "Sir Wilber",
        new BookPageInfo(
            "Greetings Traveler!",
            "I would like to invite",
            "you to a little game.",
            "See the magic key? It "
        ),
        new BookPageInfo(
            "will grant you access to",
            "the Puzzle Room. Be advised",
            "that once you take the key,",
            "you will have no more than",
            "30 minutes to enter the room."
        ),
        new BookPageInfo(
            "and solve the puzzles. If",
            "you fail, you will be",
            "expelled and all your",
            "progress will be lost! ",
            "There are 3 puzzle chests.",
            "Two of them must be completed",
            "first to unlock the third."
        ),
        new BookPageInfo(
            "If successful, you will get a",
            "special item required to enter",
            "my other playground should you",
            "discover its location within",
            "the underworld!!"
        )
    );

    [Constructible]
    public PuzzleBook() : base(false)
    {
        Movable = false;
        ItemID = 4030;
    }

    public override BookContent DefaultContent => Content;
}
