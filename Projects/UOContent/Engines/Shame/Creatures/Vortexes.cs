using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Creatures.cs) — the two final-level vortices
///     that drag attackers next to them instead of fighting at range.
///
///     The teleport cooldown was a <c>DateTime NextTeleport</c> property compared against
///     <c>DateTime.UtcNow</c>; it is a plain <see cref="Core.Now" /> field here, never serialized (the
///     original did not serialize it either, so a restart simply re-arms it).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ChaosVortex : BaseCreature
{
    public override string CorpseName => "труп вихря хаоса";

    private DateTime _nextTeleport;

    [Constructible]
    public ChaosVortex() : base(AIType.AI_Melee, FightMode.Weakest, 10, 1)
    {
        Name = "вихрь хаоса";
        Body = 164;
        Hue = 34212;

        SetStr(450);
        SetDex(200);
        SetInt(100);

        SetHits(27000);
        SetMana(0);

        SetDamage(21, 23);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 65, 75);
        SetResistance(ResistanceType.Cold, 65, 75);
        SetResistance(ResistanceType.Poison, 65, 75);
        SetResistance(ResistanceType.Energy, 65, 75);

        SetSkill(SkillName.MagicResist, 100, 110);
        SetSkill(SkillName.Tactics, 110, 130);
        SetSkill(SkillName.Wrestling, 124, 140);

        Fame = 22500;
        Karma = -22500;
    }

    public override bool AlwaysMurderer => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;

    public override int GetAngerSound() => 0x15;

    public override int GetAttackSound() => 0x28;

    public override void AlterSpellDamageFrom(Mobile from, ref int damage)
    {
        if (from is BaseCreature creature && (creature.Summoned || creature.Controlled))
        {
            damage /= 2;
        }

        if (_nextTeleport <= Core.Now)
        {
            DoTeleport(from);
        }

        base.AlterSpellDamageFrom(from, ref damage);
    }

    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        if (from is BaseCreature creature && (creature.Summoned || creature.Controlled))
        {
            damage /= 2;
        }

        if (_nextTeleport <= Core.Now)
        {
            DoTeleport(from);
        }

        base.AlterMeleeDamageFrom(from, ref damage);
    }

    public void DoTeleport(Mobile m)
    {
        var map = Map;

        if (map != null && !InRange(m.Location, 1))
        {
            var p = Point3D.Zero;

            for (var i = 0; i < 25; i++)
            {
                var x = Utility.RandomMinMax(X - 1, X + 1);
                var y = Utility.RandomMinMax(Y - 1, Y + 1);
                var z = map.GetAverageZ(x, y);

                if (map.CanSpawnMobile(x, y, z) && (x != X || y != Y))
                {
                    p = new Point3D(x, y, z);
                    break;
                }
            }

            if (p == Point3D.Zero)
            {
                p = Location;
            }

            var from = m.Location;

            Effects.SendLocationParticles(EffectItem.Create(from, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
            Effects.SendLocationParticles(EffectItem.Create(p, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

            m.MoveToWorld(p, map);

            m.PlaySound(0x1FE);
        }

        _nextTeleport = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.33 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(5));
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class UnboundEnergyVortex : BaseCreature
{
    public override string CorpseName => "труп свободного энергетического вихря";

    private DateTime _nextTeleport;

    [Constructible]
    public UnboundEnergyVortex() : base(AIType.AI_Melee, FightMode.Weakest, 10, 1)
    {
        Name = "свободный энергетический вихрь";
        Body = 13;

        SetStr(450);
        SetDex(200);
        SetInt(100);

        SetHits(20000);
        SetMana(0);

        SetDamage(21, 23);

        SetDamageType(ResistanceType.Physical, 0);
        SetDamageType(ResistanceType.Energy, 100);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 65, 75);
        SetResistance(ResistanceType.Cold, 65, 75);
        SetResistance(ResistanceType.Poison, 65, 75);
        SetResistance(ResistanceType.Energy, 100);

        SetSkill(SkillName.MagicResist, 100, 110);
        SetSkill(SkillName.Tactics, 110, 130);
        SetSkill(SkillName.Wrestling, 124, 140);

        Fame = 22500;
        Karma = -22500;
    }

    public override bool AlwaysMurderer => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;

    public override int GetAngerSound() => 0x15;

    public override int GetAttackSound() => 0x28;

    public override void AlterSpellDamageFrom(Mobile from, ref int damage)
    {
        if (from is BaseCreature creature && (creature.Summoned || creature.Controlled))
        {
            damage /= 2;
        }

        if (_nextTeleport <= Core.Now)
        {
            DoTeleport(from);
        }

        base.AlterSpellDamageFrom(from, ref damage);
    }

    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        if (from is BaseCreature creature && (creature.Summoned || creature.Controlled))
        {
            damage /= 2;
        }

        if (_nextTeleport <= Core.Now)
        {
            DoTeleport(from);
        }

        base.AlterMeleeDamageFrom(from, ref damage);
    }

    public void DoTeleport(Mobile m)
    {
        var map = Map;

        if (map != null && !InRange(m.Location, 1))
        {
            var p = Point3D.Zero;

            for (var i = 0; i < 25; i++)
            {
                var x = Utility.RandomMinMax(X - 1, X + 1);
                var y = Utility.RandomMinMax(Y - 1, Y + 1);
                var z = map.GetAverageZ(x, y);

                if (map.CanSpawnMobile(x, y, z) && (x != X || y != Y))
                {
                    p = new Point3D(x, y, z);
                    break;
                }
            }

            if (p == Point3D.Zero)
            {
                p = Location;
            }

            var from = m.Location;

            Effects.SendLocationParticles(EffectItem.Create(from, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
            Effects.SendLocationParticles(EffectItem.Create(p, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

            m.MoveToWorld(p, map);

            m.PlaySound(0x1FE);
        }

        _nextTeleport = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.33 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(5));
        }

        if (0.2 > Utility.RandomDouble())
        {
            c.DropItem(new VoidCore());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}
