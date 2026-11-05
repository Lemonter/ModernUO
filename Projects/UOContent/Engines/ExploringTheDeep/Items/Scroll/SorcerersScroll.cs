using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Scroll/SorcerersScroll.cs). One of the
///     readable documents scattered around the quest areas; the text itself is entirely client-side
///     cliloc, so only the layout and the page it opens are code. ServUO also registered a
///     <c>[SorcerersScrollGump]</c> game master command per document; those are dropped — they were
///     a testing convenience for content these commands place directly.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SorcerersScroll : Item
{
    [Constructible]
    public SorcerersScroll() : base(0x46B2)
    {
        Hue = 33;
        Movable = false;
    }

    public override int LabelNumber => 1023637; // scroll

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.HasGump<SorcerersScrollGump>())
        {
            from.SendGump(new SorcerersScrollGump());
        }
    }
}

public class SorcerersScrollGump : Gump
{
    public SorcerersScrollGump() : base(50, 50)
    {
        Closable = true;
        Disposable = true;
        Draggable = true;

        AddPage(0);
        AddBackground(6, 11, 390, 324, 9380);
        AddHtmlLocalized(60, 60, 250, 24, 1154328, 1062086, false, false);
        AddHtmlLocalized(147, 90, 250, 24, 1154415, 1062086, false, false);
        AddHtmlLocalized(42, 120, 323, 174, 1154329, 1, false, true);
    }
}
