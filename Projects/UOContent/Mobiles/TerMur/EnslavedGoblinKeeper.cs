using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/EnslavedGoblinKeeper.cs). Same
/// LootPack.LootItem&lt;T&gt; fix as EnslavedGrayGoblin.cs.</summary>
[SerializationGenerator(0, false)]
[CorpseName("an goblin corpse")]
public partial class EnslavedGoblinKeeper : BaseCreature
{
    [Constructible]
    public EnslavedGoblinKeeper() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 334;
        BaseSoundID = 0x600;

        SetStr(297, 297);
        SetDex(80, 80);
        SetInt(118, 118);

        SetHits(174, 174);
        SetStam(80, 80);
        SetMana(118, 118);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 47, 47);
        SetResistance(ResistanceType.Fire, 37, 37);
        SetResistance(ResistanceType.Cold, 29, 29);
        SetResistance(ResistanceType.Poison, 10, 11);
        SetResistance(ResistanceType.Energy, 19, 19);

        SetSkill(SkillName.MagicResist, 121.6, 122.2);
        SetSkill(SkillName.Tactics, 80.0, 82.8);
        SetSkill(SkillName.Anatomy, 82.0, 84.8);
        SetSkill(SkillName.Wrestling, 99.2, 100.7);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "порабощённый гоблин-смотритель";

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
