using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2D71, 0x2D72)]
[SerializationGenerator(0, false)]
public partial class OrnateElvenTapestry : Item
{
    [Constructible]
    public OrnateElvenTapestry() : base(0x2D72)
    {
        Weight = 1;
    }

    public override int LabelNumber => 1031633; // ornate elven tapestry
}
