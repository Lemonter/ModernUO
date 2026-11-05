using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoBridleSouth : Item
{
    [Constructible]
    public DecoBridleSouth() : base(0x1374)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class DecoBridleEast : Item
{
    [Constructible]
    public DecoBridleEast() : base(0x1375)
    {
        Movable = true;
        Stackable = false;
    }
}
