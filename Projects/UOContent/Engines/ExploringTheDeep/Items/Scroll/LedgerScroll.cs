using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Scroll/LedgerScroll.cs). One of the
///     readable documents scattered around the quest areas; the text itself is entirely client-side
///     cliloc, so only the layout and the page it opens are code. ServUO also registered a
///     <c>[LedgerGump]</c> game master command per document; those are dropped — they were
///     a testing convenience for content these commands place directly.
/// </summary>
[SerializationGenerator(0, false)]
public partial class LedgerScroll : Item
{
    [Constructible]
    public LedgerScroll() : base(0x46AF)
    {
        Movable = false;
    }

    public override int LabelNumber => 1023637; // scroll

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.HasGump<LedgerGump>())
        {
            from.SendGump(new LedgerGump());
        }
    }
}

public class LedgerGump : Gump
{
    public LedgerGump() : base(50, 50)
    {
        Closable = true;
        Disposable = true;
        Draggable = true;

        AddPage(0);
        AddBackground(6, 11, 390, 324, 9380);
        AddHtmlLocalized(178, 61, 250, 24, 1154407, 1062086, false, false);
        AddHtmlLocalized(170, 90, 250, 24, 1154420, 1062086, false, false);
        AddHtmlLocalized(42, 121, 323, 174, 1154408, 1, false, true);
    }
}
