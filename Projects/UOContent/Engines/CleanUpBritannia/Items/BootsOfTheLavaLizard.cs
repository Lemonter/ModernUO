using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BootsOfTheLavaLizard : Boots
{
    [Constructible]
    public BootsOfTheLavaLizard()
    {
        Hue = 0x674;
        Resistances.Fire = 2;
    }

    public override int LabelNumber => 1151207; // Boots Of The Lava Lizard
}
