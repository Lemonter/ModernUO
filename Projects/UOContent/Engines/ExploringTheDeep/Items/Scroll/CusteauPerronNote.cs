using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/Scroll/CusteauPerronNote.cs). One of the
///     readable documents scattered around the quest areas; the text itself is entirely client-side
///     cliloc, so only the layout and the page it opens are code. ServUO also registered a
///     <c>[CusteauPerronNoteGump]</c> game master command per document; those are dropped — they were
///     a testing convenience for content these commands place directly.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CusteauPerronNote : Item
{
    [Constructible]
    public CusteauPerronNote() : base(0x46B2)
    {
    }

    public override int LabelNumber => 1023637; // scroll

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.HasGump<CusteauPerronNoteGump>())
        {
            from.SendGump(new CusteauPerronNoteGump());
        }
    }
}

public class CusteauPerronNoteGump : Gump
{
    public CusteauPerronNoteGump() : base(50, 50)
    {
        Closable = true;
        Disposable = true;
        Draggable = true;

        AddPage(0);
        AddBackground(6, 11, 390, 324, 9380);
        AddHtmlLocalized(104, 61, 250, 24, 1154262, 1062086, false, false);
        AddHtmlLocalized(42, 90, 330, 174, 1154263, 1, false, true);
    }
}
