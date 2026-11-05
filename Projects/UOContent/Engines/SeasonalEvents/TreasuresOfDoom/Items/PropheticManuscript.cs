using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/PropheticManuscript.cs).
///     ServUO's <c>BaseJournal</c> (a journal gump bound to a title/body cliloc pair) does not exist here, so the
///     manuscript reads its cliloc page out in the localised speech of the reader instead of opening a book gump.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PropheticManuscript : Item
{
    [SerializableField(0)]
    private int _index;

    public override int LabelNumber => 1155631; // Prophetic Manuscript

    public TextDefinition Title => 1155638 + _index;

    public TextDefinition Body => 1155632 + _index;

    [Constructible]
    public PropheticManuscript() : this(Utility.RandomMinMax(0, 4))
    {
    }

    [Constructible]
    public PropheticManuscript(int index) : base(0x42BF)
    {
        _index = index;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (Body.Number > 0)
        {
            from.SendLocalizedMessage(Body.Number);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1154760, "#1155637"); // By: Owain the Blind Prophet
    }
}
