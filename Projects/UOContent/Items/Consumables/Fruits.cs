using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Остальные фрукты из этого файла в репозитории уже есть,
///     здесь только отсутствующая слива.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Plum : Food
{
    [Constructible]
    public Plum(int amount = 1) : base(0x9E86, amount)
    {
        Weight = 1.0;
        FillFactor = 1;
    }

    public override int LabelNumber => 1157208; // plum
}
