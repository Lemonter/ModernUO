using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA4B2, 0xA4B1)]
[SerializationGenerator(0, false)]
public partial class DoveCage : Item, IDyable
{
    private static readonly string[] _names =
    {
        "Белокрылый", "Джоби", "Золотое Сердце", "Белокончик", "Ошейниковый", "Обыкновенный", "Рыжий", "Толстоклювый",
        "Сулавесский", "Драконова Черепаха", "Хохлатый", "Сердцедрев", "Серолобый", "Бронзовокрылый", "Седоголовый",
        "Бурая Кукушка", "Красная Драконова Черепаха", "Скалистый", "Намаква", "Хохотун", "Плечистый", "Свистящий",
        "Зенайда", "Красноглазый", "Инка", "Ушастый", "Лесной"
    };

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _displayName;

    [Constructible]
    public DoveCage() : base(0xA4B2) => _displayName = _names[Utility.Random(_names.Length)];

    public override int LabelNumber => 1126185; // cage

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!string.IsNullOrEmpty(_displayName))
        {
            list.Add(1159261, _displayName); // A Cage With A Single ~1_WHERE~ Dove
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 3))
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else
        {
            Effects.PlaySound(m.Location, m.Map, 1663);
        }
    }
}
