using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SnakeSkinBoots : Boots
{
    [Constructible]
    public SnakeSkinBoots()
    {
        Hue = 0x7D9;
        Resistances.Poison = 2;
    }

    public override int LabelNumber => 1080122; // Snake Skin Boots
}
