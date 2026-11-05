/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/Items/WheatWort.cs).
///
///     ServUO носил версию 1 только ради того, чтобы старые сейвы с потерянным ItemID получили график
///     0x1848. Здесь класс новый, поэтому версия 0 и никакой миграции не нужно.
/// </summary>
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WheatWort : Item
{
    [Constructible]
    public WheatWort() : this(1)
    {
    }

    [Constructible]
    public WheatWort(int amount) : base(0x1848)
    {
        Stackable = true;
        Amount = amount;
        Hue = 1281;
    }

    public override int LabelNumber => 1150275; // wheat wort
}
