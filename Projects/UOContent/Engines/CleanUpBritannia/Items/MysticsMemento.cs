using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MysticsMemento : BaseTalisman
{
    [Constructible]
    public MysticsMemento() : base(0x2F5B)
    {
        Hue = 1912;
        SkillBonuses.SetValues(0, SkillName.Focus, 10.0);
        Attributes.RegenMana = 1;
        Attributes.LowerRegCost = 10;
        Attributes.SpellDamage = 5;
    }

    public override int LabelNumber => 1154730; // Mystic's Memento

    public override bool ForceShowName => true;
}
