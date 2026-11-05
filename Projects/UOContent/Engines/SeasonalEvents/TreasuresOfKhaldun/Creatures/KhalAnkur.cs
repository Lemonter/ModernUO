using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Creatures/KhalAnkur.cs).
///     Dropped: <c>OnChampPopped</c> and the <c>ChampionSpawn</c> back-reference (this codebase's
///     <c>ChampionSpawn</c> has no pop hook and <c>ChampionSpawnType.Khaldun</c> is not in its table), so the
///     champion becomes blessed on spawn and keeps its own list of summoned generals instead of registering them
///     with a spawn; the <c>SetMagicalAbility</c> roll (a pet-training school flag with no counterpart in this
///     codebase's monster-ability framework) and the <c>Caddellite</c> damage gate (no
///     <c>OnBeforeDamage</c> / item-socket system here) stay dropped. The particle burst in <c>ClearAround</c> uses
///     <see cref="Effects" /> instead of raw <c>ParticleEffect</c> packets.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhalAnkur : BaseChampion
{
    [SerializableField(0)]
    private List<BaseCreature> _warriors;

    private DateTime _nextSpawn;
    private DateTime _nextSay;
    private DateTime _nextAbilityTime;
    private int _burstTicks;
    private TimerExecutionToken _burstToken;

    public override string CorpseName => "труп Кхала Анкура";

    [Constructible]
    public KhalAnkur() : base(AIType.AI_NecroMage)
    {
        Name = "Кхал Анкур";
        Body = 0x5C7;
        BaseSoundID = 0x301;

        SetStr(700, 800);
        SetDex(500, 600);
        SetInt(800, 900);

        SetHits(30000);

        SetDamage(28, 35);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 25);
        SetDamageType(ResistanceType.Poison, 25);

        SetResistance(ResistanceType.Physical, 90);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 80, 90);
        SetResistance(ResistanceType.Energy, 80, 90);

        SetSkill(SkillName.Wrestling, 120.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 150.0);
        SetSkill(SkillName.DetectHidden, 100.0);
        SetSkill(SkillName.Parry, 80.0, 100.0);
        SetSkill(SkillName.Necromancy, 120.0);
        SetSkill(SkillName.SpiritSpeak, 120.0);

        Fame = 28000;
        Karma = -28000;

        VirtualArmor = 80;

        Blessed = true;
    }

    public override bool Unprovokable => true;

    public override bool BleedImmune => true;

    public override Poison PoisonImmune => Poison.Lethal;

    public override bool ShowFameTitle => false;

    public override bool ClickTitle => false;

    public override bool AlwaysMurderer => true;

    public override bool AutoDispel => true;

    public override double AutoDispelChance => 1.0;

    public override ChampionSkullType SkullType => ChampionSkullType.None;

    public override Type[] UniqueList => [];

    public override Type[] SharedList => [];

    public override Type[] DecorativeList => [];

    public override MonsterStatuetteType[] StatueTypes => [];

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        if (Hue == 0 && Hits < HitsMax * 0.60 && _nextAbilityTime < Core.Now && 0.2 > Utility.RandomDouble())
        {
            Hue = 2745;
            StartBurst();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Blessed)
        {
            from.SendLocalizedMessage(1071372); // It's covered with treasure guardian's magical power. To touch it, you need to beat them!
        }

        base.OnDoubleClick(from);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Map == null)
        {
            return;
        }

        if (!Utility.InRange(Location, Home, 150))
        {
            Timer.StartTimer(TimeSpan.FromSeconds(5), () => Location = Home);
        }

        if (!Blessed)
        {
            return;
        }

        if (_nextSay < Core.Now)
        {
            PublicOverheadMessage(MessageType.Regular, 0x25, 1158752 + Utility.Random(5));

            _nextSay = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(20, 40));
        }

        if (_nextSpawn < Core.Now)
        {
            SpawnGeneral();
        }
        else if (!HasLivingGeneral())
        {
            Blessed = false;
        }
    }

    private bool HasLivingGeneral()
    {
        if (_warriors == null)
        {
            return false;
        }

        for (var i = 0; i < _warriors.Count; i++)
        {
            var warrior = _warriors[i];

            if (warrior is { Deleted: false, Alive: true })
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnGeneral()
    {
        var map = Map;
        var p = Location;

        for (var i = 0; i < 25; i++)
        {
            var x = Utility.RandomMinMax(p.X - 4, p.X + 4);
            var y = Utility.RandomMinMax(p.Y - 4, p.Y + 4);
            var z = map.GetAverageZ(x, y);

            if (map.CanSpawnMobile(x, y, z))
            {
                p = new Point3D(x, y, z);
                break;
            }
        }

        var general = new KhalAnkurWarriors(KhalAnkurWarriors.WarriorType.General);

        general.MoveToWorld(p, map);
        general.FixedParticles(0x3709, 1, 30, 9963, 13, 3, EffectLayer.Head);
        general.Home = p;

        _warriors ??= new List<BaseCreature>();
        _warriors.Add(general);

        _nextSpawn = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(120, 180));
    }

    private void StartBurst()
    {
        _burstTicks = 1;

        Timer.StartTimer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), OnBurstTick, out _burstToken);
    }

    private void OnBurstTick()
    {
        if (!Alive || Deleted || Map == null)
        {
            _burstToken.Cancel();
            return;
        }

        if (_burstTicks < 15)
        {
            var p = FindLocation(Map, Location, 7);
            Effects.SendLocationEffect(p, Map, 0x3789, 30, 1, 2062, 0x4);

            _burstTicks++;
            return;
        }

        _burstToken.Cancel();
        ClearAround();
    }

    private static Point3D FindLocation(Map map, Point3D center, int range)
    {
        int cx = center.X;
        int cy = center.Y;

        for (var i = 0; i < 20; ++i)
        {
            var x = cx + Utility.Random(range * 2) - range;
            var y = cy + Utility.Random(range * 2) - range;

            if ((cx - x) * (cx - x) + (cy - y) * (cy - y) > range * range)
            {
                continue;
            }

            var z = map.GetAverageZ(x, y);

            if (!map.CanFit(x, y, z, 6, false, false))
            {
                continue;
            }

            var topZ = z;

            foreach (var item in map.GetItemsInRange(new Point3D(x, y, z), 0))
            {
                topZ = Math.Max(topZ, item.Z + item.ItemData.CalcHeight);
            }

            return new Point3D(x, y, topZ);
        }

        return center;
    }

    private void ClearAround()
    {
        var map = Map;

        if (map != null)
        {
            Server.Misc.Geometry.Circle2D(Location, map, 6, (pnt, m) =>
                {
                    if (m.CanFit(pnt, 0) && InLOS(pnt) && Utility.RandomBool())
                    {
                        Effects.SendLocationParticles(
                            EffectItem.Create(pnt, m, EffectItem.DefaultDuration),
                            0x3789,
                            1,
                            30,
                            9502
                        );
                    }
                }
            );
        }

        Timer.StartTimer(TimeSpan.FromMilliseconds(500), MoveAndDamage);
    }

    private void MoveAndDamage()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        var points = new List<Point3D>();

        Server.Misc.Geometry.Circle2D(Location, map, 7, (pnt, m) =>
            {
                if (m.CanFit(pnt, 0) && InLOS(pnt))
                {
                    points.Add(pnt);
                }
            }
        );

        if (points.Count == 0)
        {
            Hue = 0;
            return;
        }

        foreach (var from in GetMobilesInRange<Mobile>(6))
        {
            if (!from.Alive || from == this || from.AccessLevel > AccessLevel.Player)
            {
                continue;
            }

            if (from is not PlayerMobile &&
                from is not BaseCreature { Controlled: true } &&
                from is not BaseCreature { Summoned: true })
            {
                continue;
            }

            from.MoveToWorld(points[Utility.Random(points.Count)], map);
            from.Frozen = true;

            Timer.StartTimer(
                TimeSpan.FromSeconds(3),
                () =>
                {
                    from.Frozen = false;
                    from.SendLocalizedMessage(1005603); // You can move again!
                }
            );

            if (!CanBeHarmful(from))
            {
                continue;
            }

            var damage = from.Hits * 0.6;

            if (damage < 10.0)
            {
                damage = 10.0;
            }
            else if (damage > 75.0)
            {
                damage = 75.0;
            }

            DoHarmful(from);

            AOS.Damage(from, this, (int)damage, 100, 0, 0, 0, 0, 0, 0);
        }

        Hue = 0;

        _nextAbilityTime = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(200, 300));
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 3);
        AddLoot(LootPack.Meager);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _burstToken.Cancel();

        if (_warriors == null)
        {
            return;
        }

        foreach (var warrior in _warriors)
        {
            warrior?.Delete();
        }

        _warriors = null;
    }
}
