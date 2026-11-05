using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Почтовый ящик-белка. Базовый Mailbox в этом репозитории хранит идентификаторы графики
///     константами, поэтому поворот и смена вида «пустой/полный» реализованы здесь.
/// </summary>
[Furniture]
[Flippable]
[SerializationGenerator(0, false)]
public partial class SquirrelMailbox : Mailbox
{
    private const int SouthFullId = 0xA208;
    private const int SouthEmptyId = 0xA209;
    private const int EastFullId = 0xA206;
    private const int EastEmptyId = 0xA207;

    [Constructible]
    public SquirrelMailbox() : base(EastEmptyId)
    {
    }

    public override int LabelNumber => 1113927; // Mailbox

    public override int DefaultGumpID => 0x6D4;

    public void Flip()
    {
        var east = ItemID is EastFullId or EastEmptyId;
        ItemID = east
            ? IsEmpty ? SouthEmptyId : SouthFullId
            : IsEmpty ? EastEmptyId : EastFullId;
    }

    public override void OnItemAdded(Item item)
    {
        base.OnItemAdded(item);
        UpdateId();
    }

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);
        UpdateId();
    }

    private void UpdateId()
    {
        var east = ItemID is EastFullId or EastEmptyId;
        ItemID = IsEmpty
            ? east ? EastEmptyId : SouthEmptyId
            : east ? EastFullId : SouthFullId;
    }
}
