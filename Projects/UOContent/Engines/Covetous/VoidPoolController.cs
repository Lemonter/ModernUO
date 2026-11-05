using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VoidPool;

public enum VoidType
{
    Abyss,
    Repond,
    Undead,
    Reptile,
    Elemental
}

/// <summary>
///     Ported from ServUO's Covetous void pool (VoidPoolController.cs) — the item that runs the
///     endless wave defence at Covetous level 3: it opens the battle when a player is inside the
///     region, spawns a wave every 30-90 seconds from one of five creature families, damages the pool
///     walls while enemies stand next to them, and pays the defenders when the walls fall.
///
///     What this port left out, all deliberate:
///       - <c>Level3Spawner</c> — ServUO's bespoke "spawners for the dungeon's third level" wrapper with
///         its own spawn table and ISpawner plumbing. Dungeon population here comes from the normal
///         spawner system (see the Despise port's <c>[XmlLoad</c> convention), so the controller no
///         longer owns that layer.
///       - <c>PointsSystem.VoidPool</c> — this codebase has no generic points framework (the Despise
///         port made the same call and kept a small standalone tracker). Points live in this item as
///         two parallel lists and are read back through <see cref="GetPoints" /> /
///         <see cref="AwardPoints" />; Vela's reward gump spends them.
///       - The ML quest tie-in (<c>AForcedSacraficeQuest</c>) and <c>ColUtility.Free</c> (a ServUO pool
///         return with no equivalent here). Neither carries the mechanic.
///       - <c>NoLootOnDeath</c> on spawned creatures — the flag does not exist here; CovetousCreature
///         already skips loot while its <c>VoidSpawn</c> flag is set, which is how these are built.
///
///     Timing uses <see cref="Core.Now" /> (wall clock), as the original did — wave pacing is meant to
///     follow real minutes, and the countdown survives a save/load through the serialized deadlines.
/// </summary>
[SerializationGenerator(0, false)]
public partial class VoidPoolController : Item
{
    private const int RestartSpan = 15;
    private const int PoolStartHits = 15;
    private const int StartPointVariance = 8;

    private static readonly Point3D StartPoint1 = new(5592, 2012, 0);
    private static readonly Point3D StartPoint2 = new(5466, 2007, 0);
    private static readonly Point3D EndPoint = new(5500, 1998, 5);
    private static readonly Rectangle2D PoolWalls = new(5495, 1993, 10, 10);

    public static VoidPoolController InstanceTram { get; set; }
    public static VoidPoolController InstanceFel { get; set; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _wave;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _poolHits;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _respawnMin;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _respawnMax;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private VoidType _voidType;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextStart;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextWave;

    [SerializableField(7)]
    private List<Mobile> _pointHolders;

    [SerializableField(8)]
    private List<int> _pointValues;

    [SerializableField(10)]
    private List<WayPoint> _waypointsA;

    [SerializableField(11)]
    private List<WayPoint> _waypointsB;

    private bool _onGoing;
    private bool _tickRunning;
    private TimerExecutionToken _tickToken;

    [SerializableProperty(9)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value)
            {
                return;
            }

            _active = value;

            if (!value)
            {
                StopTick();

                if (Region != null)
                {
                    Region.Unregister();
                    Region = null;
                }

                return;
            }

            if (Region == null)
            {
                Region = new VoidPoolRegion(this, Map);
                Region.Register();
            }

            StartTick();
            NextStart = Core.Now + TimeSpan.FromMinutes(RestartSpan);
            Region.SendRegionMessage(1152526, RestartSpan.ToString()); // The battle ... will begin in ~1_VALUE~ minutes.
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Stage => _wave < 2 ? 0 : Math.Max(1, _wave / 5);

    [CommandProperty(AccessLevel.GameMaster)]
    public bool OnGoing => _onGoing;

    public List<WaveInfo> Waves { get; } = new();

    public Dictionary<Mobile, long> CurrentScore { get; } = new();

    public VoidPoolRegion Region { get; private set; }

    public VoidPoolController(Map map) : base(3803)
    {
        Name = "Бассейн Пустоты — контроллер";
        Visible = false;
        Movable = false;

        PoolHits = PoolStartHits;
        RespawnMin = 60;
        RespawnMax = 90;

        _pointHolders = new List<Mobile>();
        _pointValues = new List<int>();
        _waypointsA = new List<WayPoint>();
        _waypointsB = new List<WayPoint>();

        if (map == Map.Trammel && InstanceTram == null)
        {
            InstanceTram = this;
        }
        else if (map == Map.Felucca && InstanceFel == null)
        {
            InstanceFel = this;
        }
        else
        {
            Delete();
            return;
        }

        Region = new VoidPoolRegion(this, map);
        Region.Register();

        Active = true;
    }

    private void StartTick()
    {
        if (_tickRunning)
        {
            return;
        }

        _tickRunning = true;
        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _tickToken);
    }

    private void StopTick()
    {
        if (!_tickRunning)
        {
            return;
        }

        _tickRunning = false;
        _tickToken.Cancel();
    }

    private void OnTick()
    {
        if (!_onGoing)
        {
            if (Core.Now > NextStart && Region != null && Region.GetPlayerCount() > 0)
            {
                BeginInvasion();
            }

            return;
        }

        if (Core.Now > NextWave)
        {
            SpawnWave();
        }

        var map = Map;

        if (map == null)
        {
            return;
        }

        foreach (var m in map.GetMobilesInBounds(PoolWalls))
        {
            if (!_onGoing)
            {
                break;
            }

            if (m is BaseCreature { Controlled: false, Summoned: false } && Utility.RandomDouble() > 0.25)
            {
                OnVoidWallDamaged(m);
            }
        }
    }

    private void BeginInvasion()
    {
        NextStart = DateTime.MaxValue;
        _onGoing = true;
        VoidType = (VoidType)Utility.Random(5);
        PoolHits = PoolStartHits;
        Wave = 0;

        CurrentScore.Clear();
        Waves.Clear();

        Region?.SendRegionMessage(1152527, 0x2B); // The battle for the Void Pool is beginning now!

        SpawnWave();
    }

    public void SpawnWave()
    {
        Wave++;

        Region?.SendRegionMessage(1152528, Wave.ToString()); // Wave ~1_WAVE~ of the Void Pool is attacking!

        var toSpawn = (int)Math.Ceiling(Math.Max(5, Math.Sqrt(Wave) * 2) * 1.5);
        var creatures = new List<BaseCreature>(toSpawn);

        for (var i = 0; i < toSpawn; i++)
        {
            var start = i % 2 == 0 ? StartPoint1 : StartPoint2;

            for (var j = 0; j < 25; j++)
            {
                var x = start.X + Utility.RandomMinMax(start.X - StartPointVariance / 2, start.X + StartPointVariance / 2);
                var y = start.Y + Utility.RandomMinMax(start.Y - StartPointVariance / 2, start.Y + StartPointVariance / 2);
                var z = Map.GetAverageZ(x, y);

                if (Map.CanSpawnMobile(x, y, z))
                {
                    start = new Point3D(x, y, z);
                    break;
                }
            }

            var ran = Utility.RandomMinMax(0, Stage < 10 ? 12 : Stage < 15 ? 14 : 15);

            var entry = ran switch
            {
                5 or 6 or 7 or 8    => 1,
                9 or 10 or 11       => 2,
                12 or 13            => 3,
                14 or 15            => 4,
                _                   => 0
            };

            var type = SpawnTable[(int)VoidType][entry];

            if (Activator.CreateInstance(type, Wave, true) is not BaseCreature bc)
            {
                continue;
            }

            var spawnAt = start;

            Timer.DelayCall(
                TimeSpan.FromSeconds(i * 0.75),
                () =>
                {
                    if (!_onGoing)
                    {
                        bc.Delete();
                        return;
                    }

                    bc.MoveToWorld(spawnAt, Map);
                    bc.Home = EndPoint;
                    bc.RangeHome = 1;

                    creatures.Add(bc);

                    bc.CurrentWayPoint = GetNearestWaypoint(bc);
                }
            );
        }

        var gate1 = new VoidPoolGate();
        gate1.MoveToWorld(StartPoint1, Map);
        Effects.PlaySound(StartPoint1, Map, 0x20E);

        var gate2 = new VoidPoolGate();
        gate2.MoveToWorld(StartPoint2, Map);
        Effects.PlaySound(StartPoint2, Map, 0x20E);

        Timer.DelayCall(
            TimeSpan.FromSeconds(toSpawn * 0.80),
            () =>
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(gate1.Location, gate1.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    20,
                    5042
                );
                Effects.PlaySound(gate1.GetWorldLocation(), gate1.Map, 0x201);

                Effects.SendLocationParticles(
                    EffectItem.Create(gate2.Location, gate2.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    20,
                    5042
                );
                Effects.PlaySound(gate2.GetWorldLocation(), gate2.Map, 0x201);

                gate1.Delete();
                gate2.Delete();
            }
        );

        Waves.Add(new WaveInfo(Wave, creatures));
        NextWave = GetNextWaveTime();
    }

    public WayPoint GetNearestWaypoint(Mobile m, int range = 15)
    {
        var map = Map;

        if (map == null)
        {
            return null;
        }

        var closestRange = range;
        WayPoint closest = null;

        foreach (var item in map.GetItemsInRange(m.Location, range))
        {
            if (item is not WayPoint wayPoint)
            {
                continue;
            }

            var distance = (int)m.GetDistanceToSqrt(wayPoint);

            if (distance < closestRange || closest == null)
            {
                closest = wayPoint;
                closestRange = distance;
            }
        }

        return closest;
    }

    public Item GetNearestVoidPoolWall(Mobile m)
    {
        var map = Map;

        if (map == null)
        {
            return null;
        }

        const int range = 5;
        var closestRange = range;
        Item closest = null;

        foreach (var item in map.GetItemsInRange(m.Location, range))
        {
            if (item.Name != "Бассейн Пустоты")
            {
                continue;
            }

            var distance = (int)m.GetDistanceToSqrt(item);

            if (distance < closestRange || closest == null)
            {
                closest = item;
                closestRange = distance;
            }
        }

        return closest;
    }

    public DateTime GetNextWaveTime()
    {
        if (Wave == 1)
        {
            return Core.Now + TimeSpan.FromSeconds(10);
        }

        var min = Math.Max(30, RespawnMin - Wave) + Utility.RandomMinMax(0, 10);
        var max = Math.Max(45, RespawnMax - Wave) - Utility.RandomMinMax(0, 5);

        return Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(min, max));
    }

    public void OnVoidWallDamaged(Mobile damager)
    {
        if (Utility.RandomDouble() < 0.5)
        {
            PoolHits--;
        }

        Region?.SendRegionMessage(1152529); // The Void Pool walls have been damaged! Defend the Void Pool!

        var item = GetNearestVoidPoolWall(damager);

        if (item != null)
        {
            var p = new Point3D(item.X, item.Y, item.Z + 5);
            Effects.SendLocationParticles(
                EffectItem.Create(p, item.Map, EffectItem.DefaultDuration),
                Utility.RandomList(0x36BD, 0x36B0, 0x3728),
                20,
                10,
                5044
            );
            Effects.PlaySound(p, item.Map, 0x307);
        }

        if (PoolHits <= 0 && _onGoing)
        {
            _onGoing = false;
            EndInvasion();
        }
    }

    public void EndInvasion()
    {
        Region?.SendRegionMessage(1152530); // Cora's forces have destroyed the Void Pool walls. The battle is lost!

        NextStart = Core.Now + TimeSpan.FromMinutes(RestartSpan);

        Region?.SendRegionMessage(1152526, RestartSpan.ToString());

        if (Region != null)
        {
            var players = Region.GetPlayers();

            foreach (var m in players)
            {
                var points = GetCurrentPoints(m);

                if (points > 0)
                {
                    AwardPoints(m, points);
                }

                if (CurrentScore.TryGetValue(m, out var score))
                {
                    // During the battle you helped fight back ~1_COUNT~ out of ~2_TOTAL~ waves ...
                    m.SendLocalizedMessage(
                        1152650,
                        $"{GetTotalWaves(m)}\t{Wave}\t{Wave}\t{score}"
                    );
                }
            }
        }

        ClearSpawn(true);
    }

    public void OnCreatureKilled(BaseCreature killed)
    {
        foreach (var info in Waves)
        {
            if (!info.Creatures.Contains(killed))
            {
                continue;
            }

            var list = BaseCreature.GetLootingRights(killed.DamageEntries, killed.HitsMax);
            list.Sort();

            foreach (var ds in list)
            {
                var m = ds.m_Mobile;

                if (m == null)
                {
                    continue;
                }

                if (m is BaseCreature { } creature && creature.GetMaster() is PlayerMobile master)
                {
                    m = master;
                }

                if (!info.Credit.Contains(m))
                {
                    info.Credit.Add(m);
                }

                var value = killed.Fame / 998;
                CurrentScore[m] = CurrentScore.GetValueOrDefault(m) + value;
            }

            info.Creatures.Remove(killed);

            if (info.Creatures.Count == 0)
            {
                foreach (var m in info.Credit)
                {
                    if (m is not PlayerMobile || m.Region != Region)
                    {
                        continue;
                    }

                    var award = Math.Max(0, Map == Map.Felucca ? Stage * 2 : Stage);

                    if (award > 0)
                    {
                        CurrentScore[m] = CurrentScore.GetValueOrDefault(m) + Stage * 125;
                    }
                }
            }

            if (killed.Corpse is Corpse corpse && !corpse.Deleted)
            {
                corpse.BeginDecay(TimeSpan.FromMinutes(1));
            }

            break;
        }
    }

    public int GetCurrentPoints(Mobile from)
    {
        var points = 0;

        foreach (var info in Waves)
        {
            if (info.Credit.Contains(from))
            {
                points += Map == Map.Felucca ? Stage * 2 : Stage;
            }
        }

        return points;
    }

    public int GetTotalWaves(Mobile from)
    {
        var count = 0;

        foreach (var info in Waves)
        {
            if (info.Wave > 2 && info.Credit.Contains(from))
            {
                count++;
            }
        }

        return count;
    }

    public static int GetPlayerScore(Dictionary<Mobile, long> score, Mobile m) =>
        score != null && m != null && score.TryGetValue(m, out var value) ? (int)value : 0;

    /// <summary>Points banked across battles — the currency Vela charges for the void pool rewards.</summary>
    public int GetPoints(Mobile m)
    {
        var index = _pointHolders.IndexOf(m);

        return index >= 0 ? _pointValues[index] : 0;
    }

    public void AwardPoints(Mobile m, int amount)
    {
        if (m == null || amount <= 0)
        {
            return;
        }

        var index = _pointHolders.IndexOf(m);

        if (index >= 0)
        {
            _pointValues[index] += amount;
        }
        else
        {
            _pointHolders.Add(m);
            _pointValues.Add(amount);
        }

        m.SendLocalizedMessage(1152652, amount.ToString()); // You have been awarded ~1_VAL~ points ...
    }

    public bool SpendPoints(Mobile m, int amount)
    {
        var index = _pointHolders.IndexOf(m);

        if (index < 0 || _pointValues[index] < amount)
        {
            return false;
        }

        _pointValues[index] -= amount;

        if (_pointValues[index] == 0)
        {
            _pointHolders.RemoveAt(index);
            _pointValues.RemoveAt(index);
        }

        return true;
    }

    /// <summary>Stops the dungeon's own spawners so the pool isn't flooded from outside the waves.
    /// Only the XmlSpawner side is handled: this codebase's JSON spawners have no "despawn everything"
    /// call, and the pool itself is the only place creatures are cleared from.</summary>
    public void ClearSpawners()
    {
        if (Region == null)
        {
            return;
        }

        foreach (var item in Region.GetItems())
        {
            if (item is XmlSpawner spawner)
            {
                spawner.DoReset = true;
            }
        }
    }

    public void ClearSpawn() => ClearSpawn(false);

    public void ClearSpawn(bool effects)
    {
        if (Region == null)
        {
            return;
        }

        foreach (var m in Region.GetMobiles())
        {
            if (m is not CovetousCreature)
            {
                continue;
            }

            if (effects)
            {
                Effects.SendLocationEffect(m.Location, m.Map, 0xDDA, 30, 10, 0, 0);
            }

            m.Delete();
        }
    }

    public override void Delete()
    {
        if (_onGoing)
        {
            _onGoing = false;
            EndInvasion();
        }

        StopTick();

        if (Region != null)
        {
            Region.Unregister();
            Region = null;
        }

        if (InstanceTram == this)
        {
            InstanceTram = null;
        }

        if (InstanceFel == this)
        {
            InstanceFel = null;
        }

        _pointHolders?.Clear();
        _pointValues?.Clear();
        DeleteWaypoints(_waypointsA);
        DeleteWaypoints(_waypointsB);
        Waves.Clear();
        CurrentScore.Clear();

        base.Delete();
    }

    private static void DeleteWaypoints(List<WayPoint> waypoints)
    {
        if (waypoints == null)
        {
            return;
        }

        foreach (var wayPoint in waypoints)
        {
            if (wayPoint is { Deleted: false })
            {
                wayPoint.Delete();
            }
        }

        waypoints.Clear();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _pointHolders ??= new List<Mobile>();
        _pointValues ??= new List<int>();
        _waypointsA ??= new List<WayPoint>();
        _waypointsB ??= new List<WayPoint>();

        if (Map == Map.Felucca)
        {
            InstanceFel = this;
        }
        else
        {
            InstanceTram = this;
        }

        if (_active)
        {
            Region ??= new VoidPoolRegion(this, Map);
            Region.Register();
            StartTick();
        }

        // Anything the previous session left roaming the pool is stale: the battle isn't resumed.
        Timer.DelayCall(TimeSpan.FromSeconds(10), () => { ClearSpawn(); ClearSpawners(); });
    }

    public static readonly Type[][] SpawnTable =
    {
        new[] { typeof(DaemonMongbat), typeof(GargoyleAssassin), typeof(CovetousDoppleganger), typeof(LesserOni), typeof(CovetousFireDaemon) },
        new[] { typeof(LizardmanWitchdoctor), typeof(OrcFootSoldier), typeof(RatmanAssassin), typeof(OgreBoneCrusher), typeof(TitanRockHunter) },
        new[] { typeof(AngeredSpirit), typeof(BoneSwordSlinger), typeof(VileCadaver), typeof(DiseasedLich), typeof(CovetousRevenant) },
        new[] { typeof(WarAlligator), typeof(MagmaLizard), typeof(ViciousDrake), typeof(CorruptedWyvern), typeof(CovetousWyrm) },
        new[] { typeof(CovetousEarthElemental), typeof(CovetousWaterElemental), typeof(VortexElemental), typeof(SearingElemental), typeof(VenomElemental) }
    };
}

/// <summary>ServUO's temporary shimmering gate marking where a wave enters; it deletes itself on load
/// because a gate has no meaning once its wave is gone.</summary>
[SerializationGenerator(0, false)]
public partial class VoidPoolGate : Static
{
    [Constructible]
    public VoidPoolGate() : base(0xF6C)
    {
        Light = LightType.Circle300;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization() => Delete();
}
