using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles;

/// <summary>The Twisted Weald peerless. Ported from ServUO
/// (Scripts/Mobiles/Bosses/DreadHorn.cs).
///
/// Two of its drops, DreadFlute and DreadsRevenge, have no class in this codebase and are
/// left out; everything else in the loot list is here. The parrot is absent for the same
/// reason as Lady Melisande's — LootPack.Parrot is commented out upstream.</summary>
[SerializationGenerator(0, false)]
public partial class DreadHorn : BasePeerless
{
    private long _nextTeleport;
    private long _nextStomp;

    [Constructible]
    public DreadHorn() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;

        Name = "a Dread Horn";
        Body = 257;
        BaseSoundID = 0xA8;

        SetStr(878, 993);
        SetDex(581, 683);
        SetInt(1200, 1300);

        SetHits(50000);
        SetStam(507, 669);
        SetMana(1200, 1300);

        SetDamage(21, 28);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Poison, 60);

        SetResistance(ResistanceType.Physical, 40, 55);
        SetResistance(ResistanceType.Fire, 50, 65);
        SetResistance(ResistanceType.Cold, 50, 65);
        SetResistance(ResistanceType.Poison, 65, 75);
        SetResistance(ResistanceType.Energy, 60, 75);

        SetSkill(SkillName.Wrestling, 90.0);
        SetSkill(SkillName.Tactics, 90.0);
        SetSkill(SkillName.MagicResist, 110.0);
        SetSkill(SkillName.Poisoning, 120.0);
        SetSkill(SkillName.Magery, 110.0);
        SetSkill(SkillName.EvalInt, 110.0);
        SetSkill(SkillName.Meditation, 110.0);
        SetSkill(SkillName.Spellweaving, 120.0);

        Fame = 32000;
        Karma = -32000;

        _nextTeleport = Core.TickCount;
        _nextStomp = Core.TickCount;
    }

    public override string CorpseName => "a dread horns corpse";

    public override int Hides => 10;
    public override HideType HideType => HideType.Regular;
    public override int Meat => 5;

    public override bool GivesMLMinorArtifact => true;
    public override bool Unprovokable => true;
    public override Poison PoisonImmune => Poison.Deadly;
    public override Poison HitPoison => Poison.Lethal;
    public override int TreasureMapLevel => 5;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 8);
        AddLoot(LootPack.LowScrolls, 4);
        AddLoot(LootPack.MedScrolls, 4);
        AddLoot(LootPack.HighScrolls, 4);
        AddLoot(LootPack.ArcanistScrolls, Utility.RandomMinMax(1, 6));
        AddLoot(LootPack.PeerlessResource, 8);
        AddLoot(LootPack.Talisman, 5);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        c.DropItem(new DreadHornMane());

        if (Utility.RandomDouble() < 0.6)
        {
            c.DropItem(new TaintedMushroom());
        }

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new MangledHeadOfDreadhorn());
        }

        if (Utility.RandomDouble() < 0.5)
        {
            c.DropItem(new HornOfTheDreadhorn());
        }

        if (Utility.RandomDouble() < 0.05)
        {
            c.DropItem(new PristineDreadHorn());
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null || !Alive)
        {
            return;
        }

        if (_nextTeleport <= Core.TickCount)
        {
            TeleportOpponents();
        }

        if (_nextStomp <= Core.TickCount)
        {
            HoofStomp();
        }
    }

    /// <summary>Drags whoever is nearby back to its feet and poisons them — the Weald's answer
    /// to being kited.</summary>
    private void TeleportOpponents()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in map.GetMobilesInRange<Mobile>(Location, 12))
        {
            if (m == this || !m.Player || !m.Alive || !CanBeHarmful(m, false))
            {
                continue;
            }

            queue.Enqueue(m);
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();

            m.MoveToWorld(Location, map);
            m.FixedParticles(0x376A, 9, 32, 0x13AF, EffectLayer.Waist);
            m.PlaySound(0x1FE);

            m.ApplyPoison(this, Poison.Lethal);
        }

        _nextTeleport = Core.TickCount + Utility.RandomMinMax(40000, 60000);
    }

    /// <summary>A stomp that saps stats for a minute; Magic Resistance decides how much.</summary>
    private void HoofStomp()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        PlaySound(0x2F3);
        FixedParticles(0x3728, 10, 15, 5002, EffectLayer.Waist);

        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in map.GetMobilesInRange<Mobile>(Location, 8))
        {
            if (m == this || !m.Alive || !CanBeHarmful(m, false) ||
                !SpellHelper.ValidIndirectTarget(this, m))
            {
                continue;
            }

            queue.Enqueue(m);
        }

        while (queue.Count > 0)
        {
            var m = queue.Dequeue();

            DoHarmful(m);

            var duration = TimeSpan.FromSeconds(60);
            var malus = (int)(m.Skills.MagicResist.Value / 10.0) - 10;

            if (malus >= 0)
            {
                continue;
            }

            m.AddStatMod(new StatMod(StatType.Str, "[DreadHorn] Str", malus, duration));
            m.AddStatMod(new StatMod(StatType.Dex, "[DreadHorn] Dex", malus, duration));
            m.AddStatMod(new StatMod(StatType.Int, "[DreadHorn] Int", malus, duration));

            m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
            m.PlaySound(0x1DF);
        }

        _nextStomp = Core.TickCount + Utility.RandomMinMax(60000, 80000);
    }
}
