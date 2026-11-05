using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. ICustomizableMessageItem и AddCustomizableMessageGump в этом
///     репозитории отсутствуют, поэтому надпись только выводится в свойствах и не
///     редактируется игроком.
/// </summary>
[Flippable]
[SerializationGenerator(0, false)]
public partial class ValentineBear : Item, ICustomizableMessage
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _ownerName;

    [SerializableField(1)]
    private string[] _lines;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _editEnd;

    [Constructible]
    public ValentineBear(Mobile owner) : base(Utility.RandomList(0x48E0, 0x48E2))
    {
        Weight = 1.0;
        LootType = LootType.Blessed;

        _lines = new string[3];
        _ownerName = owner.Name;
        _editEnd = DateTime.MaxValue;
    }

    public void Flip()
    {
        if (ItemID is 0x48E0 or 0x48E2)
        {
            ItemID += 1;
        }
        else
        {
            ItemID -= 1;
        }
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

        list.Add(1150295, _ownerName); // ~1_NAME~'s St. Valentine Bear

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
