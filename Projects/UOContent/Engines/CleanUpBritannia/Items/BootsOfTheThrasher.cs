using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BootsOfTheThrasher : Boots
{
    [Constructible]
    public BootsOfTheThrasher()
    {
        Hue = 0x497;
        Resistances.Physical = 2;
    }

    public override int LabelNumber => 1151210; // Boots of the Thrasher
}
