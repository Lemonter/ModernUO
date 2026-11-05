using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2FB9, 0x3173)]
[SerializationGenerator(0, false)]
public partial class CloakOfPower : BaseOuterTorso
{
    public override int LabelNumber => 1112882; // Cloak of Power

    [Constructible]
    public CloakOfPower() : base(0x2FB9)
    {
        Weight = 2.0;
        Hue = 0xFE;
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
    }
}
