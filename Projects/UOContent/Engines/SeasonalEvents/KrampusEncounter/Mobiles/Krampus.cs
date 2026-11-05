using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Mobiles/Krampus.cs).
///     ServUO's <c>SetMagicalAbility</c> roll is a pet-training school flag with no counterpart in this
///     codebase's monster-ability framework and stays dropped, as do the <c>BoneBreakerContext</c> hook and the
///     teleport-chase behaviour (<c>TeleportsTo</c>/<c>GetTeleportTarget</c>/<c>OnAfterTeleport</c> do not
///     exist). <c>SpellHelper.AcquireIndirectTargets</c> is replaced by a spatial query for the cold blast.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Krampus : BaseCreature
{
    public const int MaxSummons = 24;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _spawnLocation;

    [SerializableField(1)]
    private List<BaseCreature> _summonedHelpers;

    [SerializableField(2)]
    private List<BaseCreature> _initialSpawn;

    private DateTime _nextSpecial;
    private DateTime _lastActivity;
    private TimerExecutionToken _summonToken;

    public override string CorpseName => "труп Крампуса";

    [Constructible]
    public Krampus() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "Крампус";
        Body = 1484;

        SetStr(700, 750);
        SetDex(800, 900);
        SetInt(500, 600);

        SetHits(40000);

        SetDamage(28, 35);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 80, 90);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 70, 80);

        SetSkill(SkillName.MagicResist, 150);
        SetSkill(SkillName.Tactics, 110, 120);
        SetSkill(SkillName.Wrestling, 120.0, 125.0);
        SetSkill(SkillName.DetectHidden, 60.0, 70.0);
        SetSkill(SkillName.Parry, 60, 70);

        Fame = 30000;
        Karma = -30000;

        _nextSpecial = Core.Now;
    }

    public bool IsKrampusEncounter =>
        KrampusEncounter.Encounter != null && KrampusEncounter.Encounter.Krampus == this;

    public int TotalSummons()
    {
        if (_summonedHelpers == null)
        {
            return 0;
        }

        var count = 0;

        foreach (var helper in _summonedHelpers)
        {
            if (helper != null && helper.Alive)
            {
                count++;
            }
        }

        return count;
    }

    public void Summon(Mobile target, bool initial = false)
    {
        if (target == null || (!initial && _initialSpawn is { Count: > 0 }))
        {
            return;
        }

        var map = Map;

        if (map == null || TotalSummons() > MaxSummons)
        {
            return;
        }

        if (!initial)
        {
            MovingEffect(target, 0xA271, 10, 0, false, false, 0, 0);
        }

        var delay = TimeSpan.FromSeconds(initial ? 0.25 : 1.0);

        Timer.StartTimer(delay, () => SpawnMinions(target, initial, map), out _summonToken);

        _nextSpecial = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(20, 40));
    }

    private void SpawnMinions(Mobile target, bool initial, Map map)
    {
        if (!Alive || map == null)
        {
            return;
        }

        var count = Utility.RandomMinMax(3, 5);

        for (var i = 0; i < count; i++)
        {
            var p = Location;

            for (var j = 0; j < 10; j++)
            {
                var x = Utility.RandomMinMax(p.X - 3, p.X + 3);
                var y = Utility.RandomMinMax(p.Y - 3, p.Y + 3);
                var z = map.GetAverageZ(x, y);

                if (map.CanSpawnMobile(x, y, z))
                {
                    p = new Point3D(x, y, z);
                    break;
                }
            }

            var spawn = new KrampusMinion();

            spawn.MoveToWorld(p, map);
            spawn.Home = p;
            spawn.RangeHome = 5;
            spawn.Team = Team;
            spawn.SummonMaster = this;

            if (!initial && target is PlayerMobile)
            {
                spawn.Combatant = target;
            }

            AddHelper(spawn, initial);
        }

        if (initial)
        {
            Blessed = true;
        }
    }

    protected virtual void AddHelper(BaseCreature bc, bool initial)
    {
        if (initial)
        {
            _initialSpawn ??= new List<BaseCreature>();

            if (!_initialSpawn.Contains(bc))
            {
                _initialSpawn.Add(bc);
            }
        }
        else
        {
            _summonedHelpers ??= new List<BaseCreature>();

            if (!_summonedHelpers.Contains(bc))
            {
                _summonedHelpers.Add(bc);
            }
        }
    }

    public override int GetIdleSound() => 1589;

    public override int GetAngerSound() => 1586;

    public override int GetHurtSound() => 1588;

    public override int GetDeathSound() => 1587;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss, 5);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Blessed)
        {
            if (_initialSpawn != null)
            {
                var anyAlive = false;

                foreach (var helper in _initialSpawn)
                {
                    if (!helper.Deleted)
                    {
                        anyAlive = true;
                        break;
                    }
                }

                if (!anyAlive)
                {
                    _initialSpawn = null;
                    Blessed = false;
                }
            }
            else
            {
                Blessed = false;
            }
        }

        if (Combatant == null)
        {
            if (_spawnLocation != Point3D.Zero && _lastActivity > DateTime.MinValue &&
                _lastActivity + TimeSpan.FromHours(2) < Core.Now)
            {
                Delete();
            }

            return;
        }

        _lastActivity = Core.Now;

        if (_nextSpecial >= Core.Now || Utility.RandomDouble() > 0.25)
        {
            return;
        }

        ColdBlast();

        _nextSpecial = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 60));
    }

    /// <summary>ServUO's area blast, rebuilt on a spatial query (AcquireIndirectTargets is not available).</summary>
    private void ColdBlast()
    {
        const int baseDamage = 100;

        PlaySound(0x20D);
        PlaySound(0x20D);

        foreach (var mobile in Map.GetMobilesInRange<Mobile>(Location, 10))
        {
            if (mobile == this || mobile.Deleted || !mobile.Alive || !mobile.CanBeHarmful(this, false))
            {
                continue;
            }

            var range = (int)this.GetDistanceToSqrt(mobile);

            if (range < 1)
            {
                range = 1;
            }
            else if (range > 4)
            {
                range = 4;
            }

            mobile.PlaySound(0x20D);
            AOS.Damage(mobile, this, baseDamage / range, 0, 0, 0, 0, 0, 100, 0);

            if (Combatant == null && Utility.RandomDouble() < 0.2)
            {
                Combatant = mobile;
            }
        }
    }

    public override void OnDeath(Container c)
    {
        if (IsKrampusEncounter)
        {
            AwardEncounterLoot();
        }

        base.OnDeath(c);
    }

    private void AwardEncounterLoot()
    {
        var encounter = KrampusEncounter.Encounter;

        if (encounter == null)
        {
            return;
        }

        var rights = GetLootingRights(DamageEntries, HitsMax);

        for (var i = 0; i < rights.Count; i++)
        {
            var entry = rights[i];

            if (!entry.m_HasRight || entry.m_Mobile is not PlayerMobile m)
            {
                continue;
            }

            var ordersComplete = encounter.GetOrdersComplete(m);

            if (ordersComplete < 3 && Utility.RandomMinMax(0, 8) > ordersComplete)
            {
                continue;
            }

            Item item = Utility.Random(13) switch
            {
                0  => new KrampusCoinPurse(m.Karma),
                1  => new KrampusCoinPurse(m.Karma),
                2  => new NiceTitleDeed(),
                3  => new NaughtyTitleDeed(),
                4  => new PunisherTitleDeed(),
                5  => new RecipeScroll(586), // minion hat
                6  => new RecipeScroll(587), // minion boots
                7  => new KrampusCoinPurse(463),
                8  => new KrampusCoinPurse(588),
                9  => new KrampusPunishinList(m.Name),
                10 => new SpikedWhip(),
                11 => new BarbedWhip(),
                _  => new BladedWhip()
            };

            // За доблесть в бою с павшим чудовищем вам дарован особый предмет.
            m.SendLocalizedMessage(1156269);

            if (m.Backpack == null || !m.Backpack.TryDropItem(m, item, false))
            {
                m.BankBox?.DropItem(item);
            }
        }
    }

    public override void Delete()
    {
        base.Delete();

        if (IsKrampusEncounter)
        {
            KrampusEncounter.Encounter.OnKrampusKilled();
        }

        DeleteAll(_summonedHelpers);
        DeleteAll(_initialSpawn);

        _summonedHelpers = null;
        _initialSpawn = null;
    }

    private static void DeleteAll(List<BaseCreature> list)
    {
        if (list == null)
        {
            return;
        }

        foreach (var creature in list)
        {
            creature?.Delete();
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _summonToken.Cancel();
    }
}


