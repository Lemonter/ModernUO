using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/GrayGoblin.cs). Tribe/TribeType
/// dropped — no such concept on BaseCreature here (goblin-tribe reputation system doesn't
/// exist in this codebase). LootPack.LootItem&lt;T&gt; doesn't exist here — converted to a
/// chance-based OnDeath drop (see WolfSpider.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a goblin corpse")]
public partial class GrayGoblin : BaseCreature
{
    [Constructible]
    public GrayGoblin() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 723;
        Hue = 1900;
        BaseSoundID = 0x600;

        SetStr(258, 327);
        SetDex(62, 80);
        SetInt(103, 150);

        SetHits(159, 194);
        SetStam(62, 80);
        SetMana(103, 150);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 25, 32);
        SetResistance(ResistanceType.Poison, 10, 19);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.MagicResist, 120.9, 129.1);
        SetSkill(SkillName.Tactics, 80.6, 89.4);
        SetSkill(SkillName.Anatomy, 80.3, 89.4);
        SetSkill(SkillName.Wrestling, 96.1, 105.5);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "серый гоблин";

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
