using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Semidar's collector card, ported from ServUO
///     (Scripts/Services/Expansions/Time Of Legends/Items/CardOfSemidar.cs). Double-clicking shows
///     the card art; the seven character portraits are chosen by the server-side <see cref="CardType" />.
///
///     ServerUO shipped only the seven enumerators that have art: Dupre, Nystul, Shamino, Juonar,
///     Professor Rafkin, Minax and Krampus. The paperdoll tooltip uses the card's cliloc for each
///     face, so it stays localized by the client.
/// </summary>
[SerializationGenerator(0, false)]
[Flippable(0x9C14, 0x9C15)]
public partial class CardOfSemidar : Item
{
    public enum CardType
    {
        Dupre,
        Nystul,
        Shamino,
        Juonar,
        ProfessorRafkin,
        Minax,
        Krampus
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CardType Type
    {
        get => _type;
        set
        {
            if (_type == value)
            {
                return;
            }

            _type = value;
            InvalidateProperties();
        }
    }

    [Constructible]
    public CardOfSemidar() : this((CardType)Utility.RandomMinMax(0, 3))
    {
    }

    public CardOfSemidar(CardType type) : base(0x9C14) => _type = type;

    public override int LabelNumber => 1156395; // card of Semidar

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            return;
        }

        var gump = new Gump(100, 100);
        gump.AddImage(0, 0, _type == CardType.Krampus ? 39914 : 39904 + (int)_type);
        from.SendGump(gump);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        switch (_type)
        {
            case CardType.ProfessorRafkin:
                {
                    list.Add(1156562);
                    break;
                }
            case CardType.Minax:
                {
                    list.Add(1156981);
                    break;
                }
            case CardType.Krampus:
                {
                    list.Add(1158799);
                    break;
                }
            default:
                {
                    list.Add(1156396 + (int)_type);
                    break;
                }
        }
    }
}
