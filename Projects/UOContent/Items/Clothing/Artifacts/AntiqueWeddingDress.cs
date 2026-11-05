using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AntiqueWeddingDress : PlainDress
{
    public override int LabelNumber => 1149958;

    [Constructible]
    public AntiqueWeddingDress()
    {
        Hue = 2953;
    }
}
