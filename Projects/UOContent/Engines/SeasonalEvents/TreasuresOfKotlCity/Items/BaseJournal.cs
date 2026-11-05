using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     ServUO's <c>BaseJournal</c>/<c>BaseJournalGump</c> (Scripts/Items/Books/BaseJournal.cs) do not exist here, so
///     this is the small replacement the Kotl City journals are built on: a book that opens a framed, scrollable
///     page instead of the editable <see cref="BaseBook" />.
/// </summary>
public class BaseJournalGump : Gumps.Gump
{
    public BaseJournalGump(TextDefinition title, TextDefinition body) : base(10, 10)
    {
        AddImage(0, 0, 0x761C);

        var y = 20;

        if (title != null)
        {
            if (title.Number > 0)
            {
                AddHtmlLocalized(50, y, 450, 20, 1154645, $"#{title.Number}", 0, false, false);
            }
            else
            {
                AddHtml(50, y, 450, 20, $"<CENTER>{title.String}</CENTER>", false, false);
            }

            y += 30;
        }

        if (body.Number > 0)
        {
            AddHtmlLocalized(95, y, 380, 600, body.Number, 1, false, true);
        }
        else
        {
            AddHtml(95, y, 380, 600, body.String, false, true);
        }
    }
}

/// <summary>Replacement for ServUO's <c>BaseJournal</c>, see <see cref="BaseJournalGump" />.</summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseJournal : Item
{
    public abstract TextDefinition Title { get; }

    public abstract TextDefinition Body { get; }

    protected BaseJournal() : base(0xFBE)
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            m.SendGump(new BaseJournalGump(Title, Body));
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Title?.Number > 0)
        {
            list.Add(Title.Number);
        }
        else if (Title?.String != null)
        {
            list.Add(Title.String);
        }
    }
}
