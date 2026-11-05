using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BurningAmber : GoldRing
{
    public override int LabelNumber => 1114790; // Burning Amber

    [Constructible]
    public BurningAmber()
    {
        Hue = 1174;
        Attributes.CastRecovery = 3;
        Attributes.RegenMana = 2;
        Attributes.BonusDex = 5;
        Resistances.Fire = 20;
    }
}
