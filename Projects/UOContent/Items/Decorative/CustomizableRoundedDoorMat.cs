using ModernUO.Serialization;

namespace Server.Items;

public interface ICustomizableMessage
{
    string[] Lines { get; set; }
}

/// <summary>
///     Портировано из ServUO. ICustomizableMessageItem, AddCustomizableMessageGump и EditSign в
///     этом репозитории отсутствуют, поэтому текст только выводится в свойствах и не
///     редактируется игроком.
/// </summary>
[Furniture]
[Flippable(0x4790, 0x4791)]
[SerializationGenerator(0, false)]
public partial class CustomizableRoundedDoorMat : Item, IDyable, ICustomizableMessage
{
    [SerializableField(0)]
    private string[] _lines;

    [Constructible]
    public CustomizableRoundedDoorMat() : base(0x4790)
    {
        _lines = new string[3];
        LootType = LootType.Blessed;
    }

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1116249); // That must be in your backpack for you to use it.
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_lines == null)
        {
            return;
        }

        for (var i = 0; i < _lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(_lines[i]))
            {
                list.Add(1150301 + i, _lines[i]); // [ ~1_LINE0~ ]
            }
        }
    }
}
