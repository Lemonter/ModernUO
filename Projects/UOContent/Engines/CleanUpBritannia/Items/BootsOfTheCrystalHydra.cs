using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BootsOfTheCrystalHydra : Boots
{
    [Constructible]
    public BootsOfTheCrystalHydra()
    {
        Hue = 0x47E;
        Resistances.Energy = 2;
    }

    public override int LabelNumber => 1151209; // Boots of the Crystal Hydra
}
