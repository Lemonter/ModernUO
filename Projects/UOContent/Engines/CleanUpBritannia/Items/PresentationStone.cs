using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PresentationStone : Item
{
    [Constructible]
    public PresentationStone() : base(0x32F2)
    {
        Weight = 5.0;
    }

    public override int LabelNumber => 1154745; // Presentation Stone
}
