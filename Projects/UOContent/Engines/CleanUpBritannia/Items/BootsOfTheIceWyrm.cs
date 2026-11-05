using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BootsOfTheIceWyrm : Boots
{
    [Constructible]
    public BootsOfTheIceWyrm()
    {
        Hue = 0x482;
        Resistances.Cold = 2;
    }

    public override int LabelNumber => 1151208; // Boots of the Ice Wyrm
}
