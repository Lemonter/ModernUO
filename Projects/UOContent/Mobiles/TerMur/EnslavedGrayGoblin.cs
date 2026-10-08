using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/EnslavedGrayGoblin.cs).
/// LootPack.LootItem&lt;T&gt; doesn't exist here — converted to a chance-based OnDeath drop
/// (see WolfSpider.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("an goblin corpse")]
public partial class EnslavedGrayGoblin : BaseCreature
{
    [Constructible]
    public EnslavedGrayGoblin() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 334;
        BaseSoundID = 0x600;

        SetStr(321, 321);
        SetDex(64, 64);
        SetInt(147, 147);

        SetHits(179, 179);
        SetStam(64, 64);
        SetMana(147, 147);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 50);
        SetResistance(ResistanceType.Fire, 38, 38);
        SetResistance(ResistanceType.Cold, 32, 32);
        SetResistance(ResistanceType.Poison, 12, 12);
        SetResistance(ResistanceType.Energy, 11, 11);

        SetSkill(SkillName.MagicResist, 121.6, 121.6);
        SetSkill(SkillName.Tactics, 90.0, 90.0);
        SetSkill(SkillName.Anatomy, 82.0, 82.0);
        SetSkill(SkillName.Wrestling, 99.2, 99.2);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "порабощённый серый гоблин";

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
