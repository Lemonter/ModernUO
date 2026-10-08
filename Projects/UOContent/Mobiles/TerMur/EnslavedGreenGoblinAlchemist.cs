using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/EnslavedGreenGoblinAlchemist.cs). Same
/// LootPack.LootItem&lt;T&gt; fix as EnslavedGrayGoblin.cs.</summary>
[SerializationGenerator(0, false)]
[CorpseName("an goblin corpse")]
public partial class EnslavedGreenGoblinAlchemist : BaseCreature
{
    [Constructible]
    public EnslavedGreenGoblinAlchemist() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 723;
        BaseSoundID = 0x600;

        SetStr(289, 289);
        SetDex(72, 72);
        SetInt(113, 113);

        SetHits(196, 196);
        SetStam(72, 72);
        SetMana(113, 113);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 45, 49);
        SetResistance(ResistanceType.Fire, 50, 53);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 40, 42);
        SetResistance(ResistanceType.Energy, 15, 18);

        SetSkill(SkillName.MagicResist, 124.1, 126.2);
        SetSkill(SkillName.Tactics, 75.3, 83.6);
        SetSkill(SkillName.Anatomy, 0.0, 0.0);
        SetSkill(SkillName.Wrestling, 90.4, 94.7);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "зелёный гоблин-алхимик";

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
