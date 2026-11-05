using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SoldiersMedal : BaseTalisman
{
    [Constructible]
    public SoldiersMedal() : base(0x2F5B)
    {
        Hue = 1902;
        SkillBonuses.SetValues(0, SkillName.Tactics, 10.0);
        Attributes.AttackChance = 5;
        Attributes.RegenStam = 2;
        Attributes.WeaponDamage = 20;
    }

    public override int LabelNumber => 1154726; // Soldier's Medal

    public override bool ForceShowName => true;
}
