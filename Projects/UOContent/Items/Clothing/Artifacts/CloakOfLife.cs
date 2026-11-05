using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2FB9, 0x3173)]
[SerializationGenerator(0, false)]
public partial class CloakOfLife : BaseOuterTorso
{
    public override int LabelNumber => 1112880; // Cloak of Life

    [Constructible]
    public CloakOfLife() : base(0x2FB9)
    {
        Weight = 2.0;
        Hue = 0x21;
        Attributes.RegenHits = 1;
        Attributes.BonusHits = 3;
    }
}
