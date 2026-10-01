using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Niporailem the Thief. Ported from ServUO (Scripts/Mobiles/Normal/Niporailem.cs).
///
/// He fights two ways at once: melee him and he tears off pieces of spectral armour that come
/// at you on their own, and all the while he throws his stolen gold at whoever he is fighting —
/// a hundred-stone sack straight into the pack, every five to fifteen seconds, sometimes twice.
///
/// UniqueSAList is empty for now: all 24 pieces of the Villainous and Virtuous Epiphany sets
/// are missing from this codebase, as are the four shared artifacts. BaseSABoss rolls against
/// an empty list without complaint; fill both lists when the SA artifact tables are ported.</summary>
[SerializationGenerator(0, false)]
[CorpseName("the corpse of niporailem")]
public partial class Niporailem : BaseSABoss
{
    private long _nextTreasure;
    private long _nextSpawn;
    private int _thrown;

    // Not serialized: the spectral armour is torn off during the fight and is meaningless
    // without it, and the original's own list is only read to cap the live count and to clear
    // them on his death.
    private readonly List<BaseCreature> _helpers = [];

    [Constructible]
    public Niporailem() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1)
    {
        Title = "the Thief";
        Body = 722;

        SetStr(1000);
        SetDex(1200);
        SetInt(1200);

        SetHits(10000, 10500);

        SetDamage(15, 27);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 34, 46);
        SetResistance(ResistanceType.Fire, 0);
        SetResistance(ResistanceType.Cold, 31, 49);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 31, 49);

        SetSkill(SkillName.Wrestling, 68.8, 85.0);
        SetSkill(SkillName.Tactics, 56.1, 90.0);
        SetSkill(SkillName.MagicResist, 87.7, 93.5);
        SetSkill(SkillName.EvalInt, 90.0, 100.0);
        SetSkill(SkillName.Meditation, 20.0, 30.0);
        SetSkill(SkillName.Necromancy, 120.0);
        SetSkill(SkillName.SpiritSpeak, 120.0);
        SetSkill(SkillName.Focus, 30.0, 40.0);

        // The original's own comment: Stratics never listed one.
        PackNecroReg(12, 24);

        Fame = 15000;
        Karma = -15000;
    }

    public override string DefaultName => "Нипораилем";

    public override Type[] UniqueSAList => Type.EmptyTypes;
    public override Type[] SharedSAList => Type.EmptyTypes;

    public override int Meat => 1;
    public override bool AlwaysMurderer => true;

    public override int GetIdleSound() => 1609;
    public override int GetAngerSound() => 1606;
    public override int GetHurtSound() => 1608;
    public override int GetDeathSound() => 1607;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 6);
        AddLoot(LootPack.Gems, 6);
    }

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (_nextSpawn > Core.TickCount || LiveHelperCount() > 10)
        {
            return;
        }

        // Half as likely once he is down to his last quarter — the original's numbers.
        var chance = Hits > HitsMax / 4 ? 0.25 : 0.10;

        if (Utility.RandomDouble() <= chance)
        {
            SpawnSpectralArmour(attacker);
        }
    }

    public override void OnActionCombat()
    {
        if (Combatant is not Mobile combatant || combatant.Deleted || combatant.Map != Map ||
            !InRange(combatant, 20) || !CanBeHarmful(combatant) || !InLOS(combatant))
        {
            return;
        }

        if (_nextTreasure > Core.TickCount)
        {
            return;
        }

        ThrowTreasure(combatant);

        _thrown++;

        // 75% chance to toss a second one right after the first.
        _nextTreasure = Core.TickCount + (Utility.RandomDouble() <= 0.75 && _thrown % 2 == 1
            ? 3000
            : 5000 + (int)(10000 * Utility.RandomDouble())); // 5-15 seconds
    }

    public void SpawnSpectralArmour(Mobile m)
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        var spawned = new SpectralArmour
        {
            Team = Team,
            SummonMaster = this
        };

        var loc = Location;

        for (var j = 0; j < 10; ++j)
        {
            var x = X + Utility.Random(3) - 1;
            var y = Y + Utility.Random(3) - 1;

            if (map.CanFit(x, y, Z, 16, false, false))
            {
                loc = new Point3D(x, y, Z);
                break;
            }

            var z = map.GetAverageZ(x, y);

            if (map.CanFit(x, y, z, 16, false, false))
            {
                loc = new Point3D(x, y, z);
                break;
            }
        }

        spawned.MoveToWorld(loc, map);
        spawned.Combatant = m;

        _nextSpawn = Core.TickCount + Utility.RandomMinMax(30, 60) * 1000;

        _helpers.Add(spawned);
    }

    public override void OnAfterDelete()
    {
        DeleteSpectralArmour();
        base.OnAfterDelete();
    }

    public void DeleteSpectralArmour()
    {
        for (var i = _helpers.Count - 1; i >= 0; i--)
        {
            _helpers[i]?.Delete();
        }

        _helpers.Clear();
    }

    private int LiveHelperCount()
    {
        var count = 0;

        for (var i = 0; i < _helpers.Count; i++)
        {
            if (_helpers[i]?.Deleted == false)
            {
                count++;
            }
        }

        return count;
    }

    private void ThrowTreasure(Mobile m)
    {
        DoHarmful(m);

        MovingParticles(m, 0xEEF, 9, 0, false, true, 0, 0, 9502, 6014, 0x11D, EffectLayer.Waist, 0);

        Timer.StartTimer(
            TimeSpan.FromSeconds(1.0),
            () =>
            {
                if (m.Deleted)
                {
                    return;
                }

                m.PlaySound(0x033);
                m.AddToBackpack(new NiporailemsTreasure(this));
                m.SendLocalizedMessage(1112111); // To steal my gold? To give it freely!
            }
        );
    }
}
