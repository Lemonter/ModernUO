using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2FB9, 0x3173)]
[SerializationGenerator(0, false)]
public partial class CloakOfDeath : BaseOuterTorso
{
    public override int LabelNumber => 1112881; // Cloak of Death

    [Constructible]
    public CloakOfDeath() : base(0x2FB9)
    {
        Weight = 2.0;
        Hue = 0x966;
        Attributes.DefendChance = 3;
        Attributes.AttackChance = 3;
        Attributes.SpellDamage = 3;
    }
}
