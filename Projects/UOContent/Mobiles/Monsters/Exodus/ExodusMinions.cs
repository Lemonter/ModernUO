using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The clockwork minions of Exodus, ported from ServUO
/// (Scripts/Services/Revamped Dungeons/TheExodusEncounter/Mobiles/).
///
/// All four carry the same energy field, and the original repeats its code verbatim in all four
/// files: while the field is up nothing physical touches them and every spell lands; once they
/// drop below nine tenths of their health the field fails and it is the other way round. The
/// field comes back when they are healed. Being hit by a spell, or by anyone with a bow, makes
/// them answer with an energy bolt.
///
/// Here that lives once, on this base. The original's two "an OSI bug prevents verifying this"
/// notes are on the two pieces it was unsure of — the nine-tenths threshold and whether the
/// field regenerates — and are kept where they were.</summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseExodusMinion : BaseCreature
{
    private bool _fieldActive;

    public BaseExodusMinion() : base(AIType.AI_Melee, FightMode.Closest, 10, 1) => _fieldActive = CanUseField;

    public bool FieldActive => _fieldActive;

    // TODO: an OSI bug prevents verifying this threshold.
    public bool CanUseField => Hits >= HitsMax * 9 / 10;

    public override bool IsScaredOfScaryThings => false;
    public override bool IsScaryToPets => true;
    public override bool BardImmune => !Core.AOS;
    public override Poison PoisonImmune => Poison.Lethal;

    public override int GetIdleSound() => 0x218;
    public override int GetAngerSound() => 0x26C;
    public override int GetDeathSound() => 0x211;
    public override int GetAttackSound() => 0x232;
    public override int GetHurtSound() => 0x140;

    public override void OnKilledBy(Mobile m)
    {
        base.OnKilledBy(m);

        if (Utility.RandomDouble() < 0.1)
        {
            ExodusChest.GiveRitualItem(m);
        }
    }

    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        if (_fieldActive)
        {
            damage = 0; // no melee damage while the field is up
        }
    }

    public override void AlterSpellDamageFrom(Mobile from, ref int damage)
    {
        if (!_fieldActive)
        {
            damage = 0; // no spell damage while the field is down
        }
    }

    public override void OnDamagedBySpell(Mobile from, int amount)
    {
        if (from?.Alive == true && Utility.RandomDouble() < 0.4)
        {
            SendEBolt(from);
        }

        if (!_fieldActive)
        {
            FixedParticles(0, 10, 0, 0x2522, EffectLayer.Waist);
        }
        else if (!CanUseField)
        {
            _fieldActive = false;
            FixedParticles(0x3735, 1, 30, 0x251F, EffectLayer.Waist);
        }

        base.OnDamagedBySpell(from, amount);
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (_fieldActive)
        {
            FixedParticles(0x376A, 20, 10, 0x2530, EffectLayer.Waist);
            PlaySound(0x2F4);
            attacker.SendAsciiMessage("Your weapon cannot penetrate the creature's magical barrier");
        }

        if (attacker?.Alive == true && attacker.Weapon is BaseRanged && Utility.RandomDouble() < 0.4)
        {
            SendEBolt(attacker);
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        // TODO: an OSI bug prevents verifying whether the field regenerates.
        if (!_fieldActive && !IsHurt())
        {
            _fieldActive = true;
        }
    }

    public override bool Move(Direction d)
    {
        var moved = base.Move(d);

        if (moved && _fieldActive && Combatant != null)
        {
            FixedParticles(0, 10, 0, 0x2530, EffectLayer.Waist);
        }

        return moved;
    }

    public void SendEBolt(Mobile to)
    {
        MovingParticles(to, 0x379F, 7, 0, false, true, 0xBE3, 0xFCB, 0x211);
        to.PlaySound(0x229);
        DoHarmful(to);
        AOS.Damage(to, this, 50, 0, 0, 0, 0, 100);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => _fieldActive = CanUseField;
}

[SerializationGenerator(0, false)]
[CorpseName("a drone's corpse")]
public partial class ExodusDrone : BaseExodusMinion
{
    [Constructible]
    public ExodusDrone()
    {
        Body = 0x2F4;
        Hue = 0xA92;

        SetStr(554, 650);
        SetDex(77, 85);
        SetInt(64, 85);

        SetHits(332, 389);

        SetDamage(13, 19);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 25, 35);
        SetResistance(ResistanceType.Poison, 25, 34);
        SetResistance(ResistanceType.Energy, 25, 35);

        SetSkill(SkillName.MagicResist, 83.8, 95.4);
        SetSkill(SkillName.Tactics, 82.9, 94.9);
        SetSkill(SkillName.Wrestling, 84.7, 95.9);

        Fame = 18000;
        Karma = -18000;

        VirtualArmor = 65;

        PackItem(new PowerCrystal());
        PackItem(new ArcaneGem());
        PackItem(new ClockworkAssembly());
    }

    public override string DefaultName => "дрон Исхода";

    public override void GenerateLoot() => AddLoot(LootPack.Average);
}

[SerializationGenerator(0, false)]
[CorpseName("a sentinel's corpse")]
public partial class ExodusSentinel : BaseExodusMinion
{
    [Constructible]
    public ExodusSentinel()
    {
        Body = 0x2FB;
        Hue = 0xA92;

        SetStr(854, 934);
        SetDex(81, 90);
        SetInt(81, 105);

        SetHits(516, 564);

        SetDamage(16, 22);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 25, 35);
        SetResistance(ResistanceType.Poison, 25, 35);
        SetResistance(ResistanceType.Energy, 25, 35);

        SetSkill(SkillName.MagicResist, 91.5, 99.6);
        SetSkill(SkillName.Tactics, 91.9, 99.4);
        SetSkill(SkillName.Wrestling, 90.1, 98.9);

        Fame = 18000;
        Karma = -18000;

        VirtualArmor = 65;

        PackItem(new PowerCrystal());
        PackItem(new ArcaneGem());
        PackItem(new ClockworkAssembly());
    }

    public override string DefaultName => "страж Исхода";

    public override void GenerateLoot() => AddLoot(LootPack.Rich);
}

[SerializationGenerator(0, false)]
[CorpseName("a juggernaut's corpse")]
public partial class ExodusJuggernaut : BaseExodusMinion
{
    [Constructible]
    public ExodusJuggernaut()
    {
        Body = 0x2F0;
        Hue = 2702;

        SetStr(1506, 1565);
        SetDex(92, 99);
        SetInt(101, 126);

        SetHits(1012, 1069);

        SetDamage(19, 25);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 60, 80);
        SetResistance(ResistanceType.Fire, 60, 80);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.MagicResist, 99.1, 100.0);
        SetSkill(SkillName.Tactics, 99.1, 100.0);
        SetSkill(SkillName.Wrestling, 99.1, 100.0);

        Fame = 18000;
        Karma = -18000;

        VirtualArmor = 65;

        PackItem(new PowerCrystal());
        PackItem(new ArcaneGem());
        PackItem(new ClockworkAssembly());
    }

    public override string DefaultName => "джаггернаут Исхода";

    public override void GenerateLoot() => AddLoot(LootPack.Rich);
}

/// <summary>The lord of the minions — the same machine again, bigger, and it eats summons.
/// Its ritual-item drop is the one the original puts in GenerateLoot rather than on death; that
/// branch is idle until the ritual chain is ported (see ExodusChest).</summary>
[SerializationGenerator(0, false)]
[CorpseName("an exodus minion lord's corpse")]
public partial class ExodusMinionLord : BaseExodusMinion
{
    [Constructible]
    public ExodusMinionLord()
    {
        Body = 0x2FB;
        Hue = 0xA92;

        SetStr(1501, 1571);
        SetDex(74, 78);
        SetInt(66, 89);

        SetHits(903, 957);

        SetDamage(19, 25);

        SetResistance(ResistanceType.Physical, 65, 80);
        SetResistance(ResistanceType.Fire, 65, 80);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.MagicResist, 99.3, 99.8);
        SetSkill(SkillName.Tactics, 99.4, 100.0);
        SetSkill(SkillName.Wrestling, 99.2, 99.7);

        Fame = 18000;
        Karma = -18000;

        VirtualArmor = 65;

        PackItem(new PowerCrystal());
        PackItem(new ArcaneGem());
        PackItem(new ClockworkAssembly());
    }

    public override string DefaultName => "повелитель прислужников Исхода";

    public override bool AutoDispel => true;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Rich);
    }
}
