using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SolesOfProvidence : Sandals
{
    public override int LabelNumber => 1113376; // Soles of Providence

    [Constructible]
    public SolesOfProvidence()
    {
        Hue = 1177;
        Attributes.Luck = 80;
    }
}
