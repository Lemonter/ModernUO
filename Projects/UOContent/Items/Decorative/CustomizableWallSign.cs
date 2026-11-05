using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Интерфейс ICustomizableMessageItem и гумпы редактирования текста
///     в этом репозитории отсутствуют, поэтому надпись только выводится в свойствах и не
///     редактируется игроком.
/// </summary>
[Flippable(0x4B20, 0x4B21)]
[SerializationGenerator(0, false)]
public partial class CustomizableWallSign : Item
{
    [SerializableField(0)]
    private string[] _lines;

    [Constructible]
    public CustomizableWallSign() : base(0x4B20)
    {
        _lines = new string[3];
        LootType = LootType.Blessed;
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
