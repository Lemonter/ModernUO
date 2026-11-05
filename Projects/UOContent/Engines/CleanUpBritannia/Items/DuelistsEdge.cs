using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DuelistsEdge : BaseTalisman
{
    [Constructible]
    public DuelistsEdge() : base(0x2F58)
    {
        Hue = 1902;
        SkillBonuses.SetValues(0, SkillName.Anatomy, 10.0);
        Attributes.RegenStam = 2;
        Attributes.AttackChance = 5;
        Attributes.WeaponDamage = 20;
    }

    public override int LabelNumber => 1154727; // Duelist's Edge

    public override bool ForceShowName => true;
}
