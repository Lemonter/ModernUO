using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. RedPoinsettia и WhitePoinsettia в этом репозитории уже есть,
///     поэтому здесь только отсутствующий Poinsettia.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Poinsettia : Item
{
    [Constructible]
    public Poinsettia() : this(0)
    {
    }

    [Constructible]
    public Poinsettia(int hue) : base(0x2331)
    {
        Weight = 1.0;
        Hue = hue;
    }
}
