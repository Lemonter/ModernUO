using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LuckyCharm : BaseTalisman
{
    [Constructible]
    public LuckyCharm() : base(0x2F5B)
    {
        Hue = 1923;
        Attributes.RegenHits = 1;
        Attributes.RegenStam = 1;
        Attributes.RegenMana = 1;
        Attributes.Luck = 150;
    }

    public override int LabelNumber => 1154725; // Lucky Charm
}
