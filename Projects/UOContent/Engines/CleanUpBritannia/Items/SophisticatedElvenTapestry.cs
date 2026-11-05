using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2D6F, 0x2D70)]
[SerializationGenerator(0, false)]
public partial class SophisticatedElvenTapestry : Item
{
    [Constructible]
    public SophisticatedElvenTapestry() : base(0x2D70)
    {
        Weight = 1;
    }

    public override int LabelNumber => 1151222; // sophisticated elven tapestry
}
