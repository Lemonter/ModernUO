using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/EnslavedGoblinMage.cs). Same
/// LootPack.LootItem&lt;T&gt; fix as EnslavedGrayGoblin.cs.</summary>
[SerializationGenerator(0, false)]
[CorpseName("an goblin corpse")]
public partial class EnslavedGoblinMage : BaseCreature
{
    [Constructible]
    public EnslavedGoblinMage() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 334;
        BaseSoundID = 0x600;

        SetStr(297, 297);
        SetDex(94, 94);
        SetInt(510, 510);

        SetHits(174, 174);
        SetStam(94, 94);
        SetMana(510, 510);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 22, 22);
        SetResistance(ResistanceType.Fire, 36, 37);
        SetResistance(ResistanceType.Cold, 39, 39);
        SetResistance(ResistanceType.Poison, 43, 43);
        SetResistance(ResistanceType.Energy, 14, 14);

        SetSkill(SkillName.MagicResist, 121.6, 149.7);
        SetSkill(SkillName.Tactics, 80.0, 85.2);
        SetSkill(SkillName.Anatomy, 82.0, 86.6);
        SetSkill(SkillName.Wrestling, 99.2, 106.4);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "порабощённый гоблин-маг";

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
