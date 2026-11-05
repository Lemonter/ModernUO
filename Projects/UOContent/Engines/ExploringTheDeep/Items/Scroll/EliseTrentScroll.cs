using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Scroll/EliseTrentScroll.cs). One of the
///     readable documents scattered around the quest areas; the text itself is entirely client-side
///     cliloc, so only the layout and the page it opens are code. ServUO also registered a
///     <c>[EliseTrentGump]</c> game master command per document; those are dropped — they were
///     a testing convenience for content these commands place directly.
/// </summary>
[SerializationGenerator(0, false)]
public partial class EliseTrentScroll : Item
{
    [Constructible]
    public EliseTrentScroll() : base(0x46AF)
    {
        Movable = false;
    }

    public override int LabelNumber => 1023637; // scroll

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.HasGump<EliseTrentGump>())
        {
            from.SendGump(new EliseTrentGump());
        }
    }
}

public class EliseTrentGump : Gump
{
    public EliseTrentGump() : base(50, 50)
    {
        Closable = true;
        Disposable = true;
        Draggable = true;

        AddPage(0);
        AddBackground(6, 11, 390, 324, 9380);
        AddHtmlLocalized(130, 61, 250, 24, 1154388, 1062086, false, false);
        AddHtmlLocalized(150, 90, 250, 24, 1154376, 1062086, false, false);
        AddHtmlLocalized(42, 121, 323, 174, 1154389, 1, false, true);
    }
}
