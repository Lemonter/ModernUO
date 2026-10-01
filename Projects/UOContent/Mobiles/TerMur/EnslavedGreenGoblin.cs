using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/EnslavedGreenGoblin.cs). Same
/// LootPack.LootItem&lt;T&gt; fix as EnslavedGrayGoblin.cs.</summary>
[SerializationGenerator(0, false)]
[CorpseName("an goblin corpse")]
public partial class EnslavedGreenGoblin : BaseCreature
{
    [Constructible]
    public EnslavedGreenGoblin() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 334;
        BaseSoundID = 0x600;

        SetStr(326, 326);
        SetDex(71, 71);
        SetInt(126, 126);

        SetHits(184, 184);
        SetStam(71, 71);
        SetMana(126, 126);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 40);
        SetResistance(ResistanceType.Fire, 38, 39);
        SetResistance(ResistanceType.Cold, 31, 32);
        SetResistance(ResistanceType.Poison, 12, 12);
        SetResistance(ResistanceType.Energy, 10, 11);

        SetSkill(SkillName.MagicResist, 121.6, 122.9);
        SetSkill(SkillName.Tactics, 80.0, 81.2);
        SetSkill(SkillName.Anatomy, 82.0, 83.4);
        SetSkill(SkillName.Wrestling, 99.2, 99.4);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "порабощённый зелёный гоблин";

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
