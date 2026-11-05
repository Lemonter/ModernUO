using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

/// <summary>
///     Почтовый ящик из награды Clean Up Britannia. Показывает имя того, кто положил предмет,
///     если ящик закреплён в доме и у открывающего нет к нему доступа.
/// </summary>
[Furniture]
[DynamicFlipping]
[Flippable(0x4141, 0x4143)]
[SerializationGenerator(0, false)]
public partial class Mailbox : LockableContainer
{
    public const int SouthMailBoxID = 0x4141;
    public const int SouthEmptyMailBoxID = 0x4142;
    public const int EastMailBoxID = 0x4143;
    public const int EastEmptyMailBoxID = 0x4144;

    [SerializableField(0, getter: "private", setter: "private")]
    private Dictionary<Item, Mobile> _contents;

    [Constructible]
    public Mailbox() : this(0x4142)
    {
    }

    [Constructible]
    public Mailbox(int itemID) : base(itemID)
    {
        Weight = 5.0;
    }

    public override int LabelNumber => 1113927; // Mailbox

    public override int DefaultGumpID => 0x11A;

    public bool IsEmpty => Items.Count == 0;

    public override bool IsAccessibleTo(Mobile m) => true;

    private void CheckMailBox()
    {
        if (IsEmpty)
        {
            if (ItemID == SouthMailBoxID)
            {
                ItemID = SouthEmptyMailBoxID;
            }
            else if (ItemID == EastMailBoxID)
            {
                ItemID = EastEmptyMailBoxID;
            }
        }
        else
        {
            if (ItemID == SouthEmptyMailBoxID)
            {
                ItemID = SouthMailBoxID;
            }
            else if (ItemID == EastEmptyMailBoxID)
            {
                ItemID = EastMailBoxID;
            }
        }
    }

    public override void OnItemAdded(Item item)
    {
        base.OnItemAdded(item);

        CheckMailBox();
    }

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        CheckMailBox();

        if (_contents != null && _contents.Remove(item) && _contents.Count == 0)
        {
            _contents = null;
        }
    }

    public override void GetChildProperties(IPropertyList list, Item item)
    {
        base.GetChildProperties(list, item);

        if (_contents != null && _contents.TryGetValue(item, out var dropper))
        {
            list.Add(1113938, dropper?.Name ?? "неизвестно");
        }
    }

    public override void SendFullItemsMessage(Mobile to, Item item)
    {
        to.SendLocalizedMessage(1113940); // That mailbox is completely full.
    }

    public override void OnAfterDelete()
    {
        _contents = null;

        base.OnAfterDelete();
    }

    private void NoteDropper(Mobile from, Item item)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house?.CheckSecureAccess(from, this) != SecureAccessResult.Inaccessible)
        {
            return;
        }

        _contents ??= new Dictionary<Item, Mobile>();
        _contents[item] = from;
        item.InvalidateProperties();
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (!base.OnDragDropInto(from, item, p))
        {
            return false;
        }

        NoteDropper(from, item);

        return true;
    }

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
    {
        if (!base.TryDropItem(from, dropped, sendFullMessage))
        {
            return false;
        }

        NoteDropper(from, dropped);

        return true;
    }
}
