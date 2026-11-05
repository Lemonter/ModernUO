using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WizardsCurio : BaseTalisman
{
    [Constructible]
    public WizardsCurio() : base(0x2F58)
    {
        Hue = 1912;
        SkillBonuses.SetValues(0, SkillName.EvalInt, 10.0);
        Attributes.RegenMana = 1;
        Attributes.LowerRegCost = 10;
        Attributes.SpellDamage = 5;
    }

    public override int LabelNumber => 1154729; // Wizard's Curio

    public override bool ForceShowName => true;
}
