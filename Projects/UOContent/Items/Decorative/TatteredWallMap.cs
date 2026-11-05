using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TatteredWallMapSouth : Item
{
    [Constructible]
    public TatteredWallMapSouth() : base(11636)
    {
    }

    public override int LabelNumber => 1031635; // tattered wall map
}

[SerializationGenerator(0, false)]
public partial class TatteredWallMapEast : Item
{
    [Constructible]
    public TatteredWallMapEast() : base(11635)
    {
    }

    public override int LabelNumber => 1031635; // tattered wall map
}
