using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/GrayGoblinKeeper.cs). Tribe/TribeType
/// dropped — see GrayGoblin.cs. LootPack.LootItem&lt;T&gt; doesn't exist here — converted to a
/// chance-based OnDeath drop (see WolfSpider.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a goblin keeper corpse")]
public partial class GrayGoblinKeeper : BaseCreature
{
    [Constructible]
    public GrayGoblinKeeper() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 723;
        Hue = 1900;
        BaseSoundID = 0x600;

        SetStr(326);
        SetDex(79);
        SetInt(114);

        SetHits(186);
        SetStam(79);
        SetMana(114);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 45);
        SetResistance(ResistanceType.Fire, 33);
        SetResistance(ResistanceType.Cold, 25);
        SetResistance(ResistanceType.Poison, 20);
        SetResistance(ResistanceType.Energy, 10);

        SetSkill(SkillName.MagicResist, 129.9);
        SetSkill(SkillName.Tactics, 86.7);
        SetSkill(SkillName.Anatomy, 86.6);
        SetSkill(SkillName.Wrestling, 103.6);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "серый гоблин-смотритель";

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
