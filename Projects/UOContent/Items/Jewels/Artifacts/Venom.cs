using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Venom : GoldBracelet
{
    public override int LabelNumber => 1114783; // Venom

    [Constructible]
    public Venom()
    {
        Hue = 1371;
        Attributes.CastRecovery = 1;
        Attributes.CastSpeed = 2;
        Attributes.SpellDamage = 10;
        Resistances.Poison = 20;
    }
}
