using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Остальные типы из этого файла в репозитории уже есть, здесь только
///     отсутствующие.
/// </summary>
public static class UtilityItem
{
    public static int RandomChoice(int itemID1, int itemID2) => Utility.Random(2) == 0 ? itemID1 : itemID2;
}

[SerializationGenerator(0, false)]
public partial class SackFlourOpen : Item
{
    [Constructible]
    public SackFlourOpen() : base(0x103A) => Weight = 4.0;

    public override int LabelNumber => 1024166; // open sack of flour
}
