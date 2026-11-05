using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/GargoyleGuardian.cs). The thrown hatchet is
/// the original's own helper (moving effect, harmful act, 50 physical), kept as-is.</summary>
[SerializationGenerator(0, false)]
public partial class GargoyleGuardian : BaseCreature
{
    [Constructible]
    public GargoyleGuardian() : base(AIType.AI_Mage, FightMode.None, 10, 1)
    {
        Body = 0x2F3;
        BaseSoundID = 0x174;

        SetStr(760, 850);
        SetDex(102, 150);
        SetInt(152, 200);

        SetHits(482, 485);

        SetDamage(1000, 1150);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 15, 25);
        SetResistance(ResistanceType.Energy, 15, 25);

        SetSkill(SkillName.Wrestling, 90.1, 100.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 120.4, 160.0);
        SetSkill(SkillName.Anatomy, 50.5, 100.0);
        SetSkill(SkillName.Swords, 90.1, 100.0);
        SetSkill(SkillName.Macing, 90.1, 100.0);
        SetSkill(SkillName.Fencing, 90.1, 100.0);
        SetSkill(SkillName.Magery, 90.1, 100.2);
        SetSkill(SkillName.EvalInt, 90.1, 100.0);
        SetSkill(SkillName.Meditation, 90.1, 100.0);

        Fame = 10000;
        Karma = -10000;
        Frozen = true;
        CantWalk = true;
        Hue = 1153;

        VirtualArmor = 50;

        if (0.2 > Utility.RandomDouble())
        {
            PackItem(new GargoylesPickaxe());
        }
    }

    public override string CorpseName => "труп горгульи";
    public override string DefaultName => "страж бездны";

    public override bool BardImmune => !Core.AOS;
    public override int Meat => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.MedScrolls);
        AddLoot(LootPack.Gems, 2);
    }

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        if (from != null && from.Alive && 0.4 > Utility.RandomDouble())
        {
            ThrowHatchet(from);
        }
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (attacker != null && attacker.Alive && attacker.Weapon is BaseRanged && 0.4 > Utility.RandomDouble())
        {
            ThrowHatchet(attacker);
        }
    }

    public void ThrowHatchet(Mobile to)
    {
        const int damage = 50;

        MovingEffect(to, 0xF43, 10, 0, false, false);
        DoHarmful(to);
        AOS.Damage(to, this, damage, 100, 0, 0, 0, 0);
    }
}
