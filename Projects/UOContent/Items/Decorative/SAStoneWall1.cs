using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SAStoneWall1South : Item
{
    [Constructible]
    public SAStoneWall1South() : base(969)
    {
        Name = "каменная стена";
        Hue = 1110;
        Movable = false;
    }
}

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SAStoneWall1East : Item
{
    [Constructible]
    public SAStoneWall1East() : base(968)
    {
        Name = "каменная стена";
        Hue = 1110;
        Movable = false;
    }
}
