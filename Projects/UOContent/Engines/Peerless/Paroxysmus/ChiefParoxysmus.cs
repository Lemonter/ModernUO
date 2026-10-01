using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The Palace of Paroxysmus peerless. Ported from ServUO
/// (Scripts/Mobiles/Bosses/ChiefParoxysmus.cs).
///
/// Its two signature moves are both about punishing a tamer: it drags ranged attackers into
/// melee, and it eats your pets — healing itself to full and vomiting up three Bulbous
/// Putrifications a second later.
///
/// The poison aura goes through this codebase's own aura hooks (HasAura / AuraInterval /
/// AuraRange / Aura*Damage on BaseCreature) rather than a bespoke one.
///
/// Not ported: ParoxysmusSwampDragonStatue (no such class here) and the parrot, for the same
/// reason as the other peerless bosses — LootPack.Parrot is commented out upstream.</summary>
[SerializationGenerator(0, false)]
public partial class ChiefParoxysmus : BasePeerless
{
    private long _nextTeleport;

    [Constructible]
    public ChiefParoxysmus() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;

        Name = "a chief paroxysmus";
        Body = 0x100;

        SetStr(1232, 1400);
        SetDex(76, 82);
        SetInt(76, 85);

        SetHits(50000);

        SetDamage(27, 31);

        SetDamageType(ResistanceType.Physical, 80);
        SetDamageType(ResistanceType.Poison, 20);

        SetResistance(ResistanceType.Physical, 75, 85);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.Wrestling, 120.0);
        SetSkill(SkillName.Tactics, 120.0);
        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Anatomy, 120.0);
        SetSkill(SkillName.Poisoning, 120.0);

        Fame = 25000;
        Karma = -25000;

        _nextTeleport = Core.TickCount;
    }

    public override string CorpseName => "a chief paroxysmus corpse";

    public override int GetAttackSound() => 0x570;
    public override int GetDeathSound() => 0x56F;
    public override int GetIdleSound() => 0x571;
    public override int GetAngerSound() => 0x572;
    public override int GetHurtSound() => 0x573;

    public override bool GivesMLMinorArtifact => true;
    public override Poison PoisonImmune => Poison.Lethal;

    public override bool HasAura => true;
    public override int AuraBaseDamage => 10;
    public override int AuraPhysicalDamage => 0;
    public override int AuraFireDamage => 0;
    public override int AuraPoisonDamage => 100;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 8);
        AddLoot(LootPack.PeerlessResource, 8);
        AddLoot(LootPack.Talisman, 5);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new LardOfParoxysmus());

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new SweatOfParoxysmus());
        }

        switch (Utility.Random(40))
        {
            case 0:
                {
                    c.DropItem(new ParoxysmusCorrodedStein());
                    break;
                }
            case 1:
                {
                    c.DropItem(new StringOfPartsOfParoxysmusVictims());
                    break;
                }
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant != null && Alive && _nextTeleport <= Core.TickCount)
        {
            TeleportAttackers();
        }
    }

    /// <summary>Nothing fights this thing from range for long.</summary>
    private void TeleportAttackers()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in map.GetMobilesInRange<Mobile>(Location, 12))
        {
            if (m == this || !m.Player || !m.Alive || !CanBeHarmful(m, false) || InRange(m, 2))
            {
                continue;
            }

            queue.Enqueue(m);
        }

        if (queue.Count == 0)
        {
            return;
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();

            m.MoveToWorld(Location, map);
            m.FixedParticles(0x376A, 9, 32, 0x13AF, EffectLayer.Waist);
            m.PlaySound(0x1FE);
        }

        _nextTeleport = Core.TickCount + Utility.RandomMinMax(20000, 40000);
    }

    /// <summary>Swallowing a pet or a summon heals it to full and buys the party three new
    /// enemies a second later.</summary>
    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        if (defender is not BaseCreature bc || !bc.Controlled && !bc.Summoned)
        {
            return;
        }

        if (0.1 < Utility.RandomDouble())
        {
            return;
        }

        var location = bc.Location;
        var map = bc.Map;

        bc.Delete();

        Hits = HitsMax;
        PlaySound(0x570);

        Timer.DelayCall(TimeSpan.FromSeconds(1), () => SpitOut(location, map));
    }

    private void SpitOut(Point3D location, Map map)
    {
        if (map == null || Deleted)
        {
            return;
        }

        for (var i = 0; i < 3; i++)
        {
            SpawnHelper(new BulbousPutrification(), location);
        }
    }
}
