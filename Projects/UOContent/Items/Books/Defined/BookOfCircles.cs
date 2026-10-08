using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The gargoyle scripture the Sacred Quest examines you on. Ported from ServUO
/// (Scripts/Items/Books/BookOfCircles.cs); it sits on a lectern beside the Shrine of
/// Singularity in the Tomb of Kings, next to <see cref="ShrineMantra" />.</summary>
[SerializationGenerator(0, false)]
public partial class BookOfCircles : BrownBook
{
    public static readonly BookContent Content = new(
        "Book Of Circles",
        "unknown",
        new BookPageInfo(
            "All begins with the three",
            "principles:",
            "Control Passion and",
            "Diligence."
        ),
        new BookPageInfo(
            "From Control springs",
            "Direction.",
            "From Passion springs",
            "Feeling.",
            "From Diligence springs",
            "Persistence."
        ),
        new BookPageInfo(
            "But these three are no",
            "more important than the",
            "other five: Control com",
            "bines with Passion to",
            "give Balance. Passion",
            "combines with Diligence",
            "to yield Achievement."
        ),
        new BookPageInfo(
            "And Diligence joins with",
            "Control to provide",
            "Precision.",
            "The absence of Control",
            "Passion and Diligence is",
            "Chaos."
        ),
        new BookPageInfo(
            "Thus the absence of the",
            "principles points toward",
            "the seventh virtue, Order.",
            "The three principles unify",
            "to form Singularity."
        ),
        new BookPageInfo(
            "This is the eighth virtue,",
            "but is also the first,",
            "because within Singularity",
            "can be found all the",
            "principles and thus all",
            "the virtues."
        ),
        new BookPageInfo(
            "A circle has no end",
            "It continues forever,",
            "with all parts equally",
            "important in the success",
            "of the whole."
        ),
        new BookPageInfo(
            "Our society is the same.",
            "It too continues forever,",
            "with all members (and all",
            "virtues) equal parts of",
            "The unified whole."
        )
    );

    [Constructible]
    public BookOfCircles() : base(false) => Hue = 2210;

    public override BookContent DefaultContent => Content;
}

/// <summary>The one word that wakes the Shrine of Singularity. Ported from ServUO
/// (Scripts/Items/Books/ShrineMantra.cs).</summary>
[SerializationGenerator(0, false)]
public partial class ShrineMantra : BrownBook
{
    public static readonly BookContent Content = new(
        "Shrine of Singularity Mantra",
        "Naxatillor",
        new BookPageInfo("unorus")
    );

    [Constructible]
    public ShrineMantra() : base(false) => Hue = 2210;

    public override BookContent DefaultContent => Content;
}
