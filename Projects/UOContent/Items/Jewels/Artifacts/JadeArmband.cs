using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class JadeArmband : GoldBracelet
{
    public override int LabelNumber => 1112407; // Jade Armband [Replica]

    [Constructible]
    public JadeArmband()
    {
        Hue = 2126;
        Attributes.AttackChance = 10;
        Attributes.DefendChance = 10;
        Attributes.WeaponSpeed = 5;
        Resistances.Poison = 20;
    }

    public override int InitMinHits => 150;
    public override int InitMaxHits => 150;
}
