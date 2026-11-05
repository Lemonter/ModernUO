using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SAStoneWall2South : Item
{
    [Constructible]
    public SAStoneWall2South() : base(88)
    {
        Name = "каменная стена";
        Movable = false;
    }
}

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SAStoneWall2East : Item
{
    [Constructible]
    public SAStoneWall2East() : base(87)
    {
        Name = "каменная стена";
        Movable = false;
    }
}
