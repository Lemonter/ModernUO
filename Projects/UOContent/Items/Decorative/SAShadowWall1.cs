using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SAShadowWall1South : Item
{
    [Constructible]
    public SAShadowWall1South() : base(13883)
    {
        Name = "стена тени";
        Movable = false;
    }
}

/// <summary>
///     Портировано из ServUO. DamageableItem/ItemLevel в этом репозитории отсутствуют, поэтому
///     стены сделаны обычными непередвигаемыми предметами без механики разрушения.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SAShadowWall1East : Item
{
    [Constructible]
    public SAShadowWall1East() : base(13882)
    {
        Name = "стена тени";
        Movable = false;
    }
}
