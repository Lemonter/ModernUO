using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Почтовый ящик-бочка. Базовый Mailbox в этом репозитории хранит идентификаторы графики
///     константами, поэтому поворот и смена вида «пустой/полный» реализованы здесь.
/// </summary>
[Furniture]
[Flippable]
[SerializationGenerator(0, false)]
public partial class BarrelMailbox : Mailbox
{
    private const int SouthFullId = 0xA1F8;
    private const int SouthEmptyId = 0xA1F9;
    private const int EastFullId = 0xA1F5;
    private const int EastEmptyId = 0xA1F7;

    [Constructible]
    public BarrelMailbox() : base(EastEmptyId)
    {
    }

    public override int LabelNumber => 1113927; // Mailbox

    public override int DefaultGumpID => 0x6D5;

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
