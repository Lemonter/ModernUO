using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DesCityWallSouth : Item
{
    [Constructible]
    public DesCityWallSouth() : base(641)
    {
        Name = "повреждённая стена";
        Movable = false;
    }
}

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DesCityWallEast : Item
{
    [Constructible]
    public DesCityWallEast() : base(642)
    {
        Name = "повреждённая стена";
        Movable = false;
    }
}
