using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonStump : Item
{
    [Constructible]
    public MahaonStump() : base(Utility.RandomList(0x0E57, 0x0E59))
    {
        Movable = false;
        Name = "пенёк";
    }
}
