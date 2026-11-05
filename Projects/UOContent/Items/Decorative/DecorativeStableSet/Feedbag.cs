using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0xA4E3, 0xA4E4)]
[SerializationGenerator(0, false)]
public partial class Feedbag : BaseContainer
{
    [Constructible]
    public Feedbag() : base(0xA4E3)
    {
    }

    public override int LabelNumber => 1126235; // feedbag
}
