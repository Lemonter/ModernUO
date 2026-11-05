using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NecromancersPhylactery : BaseTalisman
{
    [Constructible]
    public NecromancersPhylactery() : base(0x2F5A)
    {
        Hue = 1912;
        SkillBonuses.SetValues(0, SkillName.SpiritSpeak, 10.0);
        Attributes.RegenMana = 1;
        Attributes.LowerRegCost = 10;
        Attributes.SpellDamage = 5;
    }

    public override int LabelNumber => 1154728; // Necromancer's Phylactery

    public override bool ForceShowName => true;
}
