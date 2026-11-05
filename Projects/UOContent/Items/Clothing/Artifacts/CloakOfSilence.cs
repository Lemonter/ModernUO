using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2FB9, 0x3173)]
[SerializationGenerator(0, false)]
public partial class CloakOfSilence : BaseOuterTorso
{
    public override int LabelNumber => 1112883; // Cloak of Silence

    [Constructible]
    public CloakOfSilence() : base(0x2FB9)
    {
        Weight = 2.0;
        Hue = 0x2A0;
        SkillBonuses.SetValues(0, SkillName.Stealth, 10);
    }
}
