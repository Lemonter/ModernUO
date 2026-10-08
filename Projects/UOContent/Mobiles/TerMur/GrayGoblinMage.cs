using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/GrayGoblinMage.cs). Tribe/TribeType
/// dropped — see GrayGoblin.cs. LootPack.LootItem&lt;T&gt; doesn't exist here — converted to
/// a chance-based OnDeath drop (see WolfSpider.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a goblin mage corpse")]
public partial class GrayGoblinMage : BaseCreature
{
    [Constructible]
    public GrayGoblinMage() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 723;
        Hue = 1900;
        BaseSoundID = 0x600;

        SetStr(227, 285);
        SetDex(70, 88);
        SetInt(451, 499);

        SetHits(129, 151);
        SetStam(70, 88);
        SetMana(451, 499);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 25, 32);
        SetResistance(ResistanceType.Poison, 10, 19);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.MagicResist, 141.9, 147.1);
        SetSkill(SkillName.Tactics, 80.7, 86.9);
        SetSkill(SkillName.Anatomy, 81.9, 89.4);
        SetSkill(SkillName.Wrestling, 90.5, 104.2);
        SetSkill(SkillName.Magery, 105.5, 119.1);
        SetSkill(SkillName.EvalInt, 94.9, 107.7);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "серый гоблин-маг";

    public override int GetAngerSound() => 0x600;
    public override int GetIdleSound() => 0x600;
    public override int GetAttackSound() => 0x5FD;
    public override int GetHurtSound() => 0x5FF;
    public override int GetDeathSound() => 0x5FE;

    public override bool CanRummageCorpses => true;
    public override int TreasureMapLevel => 1;
    public override int Meat => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.2)
        {
            c.DropItem(new BolaBall());
        }
    }
}
