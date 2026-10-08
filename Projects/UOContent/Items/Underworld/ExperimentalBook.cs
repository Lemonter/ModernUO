using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Underworld/ExperimentalRoom/
// ExperimentalBook.cs) — the instruction book sitting at the Experimental Room entrance.
[SerializationGenerator(0, false)]
public partial class ExperimentalBook : BrownBook
{
    public override int LabelNumber => 1113479;

    public static readonly BookContent Content = new(
        "Read Me!", "Sir Wilber",
        new BookPageInfo(
            "Hello again!",
            "This is Part II of my",
            "experiment. Beyond these",
            "doors is a challenge for",
            "those carrying an ",
            "Experimental Gem."
        ),
        new BookPageInfo(
            "INSTRUCTIONS:",
            "Activate the Gem to start",
            "the self-destruct timer.",
            "You have 30 minutes to",
            "reach the final room."
        ),
        new BookPageInfo(
            "Each room has colored",
            "areas matching various states:",
            "White = Cold",
            "Pink = Warm",
            "Blue = Freezing",
            "Red = Blazing"
        ),
        new BookPageInfo(
            "Green = Poison",
            "Orange = Cure",
            "Dark Green= Lethal",
            "Brown=Greater Cure",
            "Your gem's state will cycle",
            "randomly through these"
        ),
        new BookPageInfo(
            "colors. Each time the gem",
            "shifts, you must enter the",
            "colored region that will",
            "bring your gem back to neutral",
            "state (grey). "
        ),
        new BookPageInfo(
            "Failing when the gem reaches",
            "the extreme of any state get you",
            "expelled from the room and you",
            "will have to start over. The",
            "self-destruct"
        ),
        new BookPageInfo(
            "timer will not reset. Once you",
            "reach the final room, place your",
            "gem in the box to receive",
            "your reward."
        )
    );

    public override BookContent DefaultContent => Content;

    [Constructible]
    public ExperimentalBook() : base(false) => ItemID = 4030;
}
