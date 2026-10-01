using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/GreenGoblin.cs). Tribe/TribeType
/// dropped — see GrayGoblin.cs.
/// LootPack.LootItem&lt;T&gt; doesn't exist here — converted to a chance-based OnDeath drop
/// (see WolfSpider.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a goblin corpse")]
public partial class GreenGoblin : BaseCreature
{
    [Constructible]
    public GreenGoblin() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 723;
        BaseSoundID = 0x600;

        SetStr(252, 343);
        SetDex(60, 74);
        SetInt(117, 148);

        SetHits(162, 208);
        SetStam(60, 74);
        SetMana(117, 148);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 30, 39);
        SetResistance(ResistanceType.Cold, 27, 35);
        SetResistance(ResistanceType.Poison, 11, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.MagicResist, 120.5, 128.8);
        SetSkill(SkillName.Tactics, 80.9, 89.9);
        SetSkill(SkillName.Anatomy, 83.1, 89.6);
        SetSkill(SkillName.Wrestling, 93.0, 108.3);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "зелёный гоблин";

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

        if (Utility.RandomDouble() < 0.01)
        {
            c.DropItem(new LuckyCoin());
        }
    }
}
