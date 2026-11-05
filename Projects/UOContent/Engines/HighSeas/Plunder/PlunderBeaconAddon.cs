/// <summary>
///     Портировано из ServUO (Scripts/Services/Seasonal Events/RisingTide/PlunderBeaconAddon.cs —
///     класс <c>PlunderBeaconAddon</c>).
///
///     Плот пиратского маяка: плавучая платформа, на которой стоит <see cref="PlunderBeacon" />,
///     несколько расчётных пушек и которую охраняют заспавненные вокруг существа.
///
///     Чем отличается от ServUO (упрощение, разрешённое заданием):
///     <list type="bullet">
///         <item>
///             Пиратских расчётов (<c>PirateCrew</c>), <c>BaseShipCaptain</c> и <c>PirateCaptain</c>
///             в этом движке нет, поэтому пушки на плоту стоят без расчёта и с
///             <c>CanFireUnmanned</c>: они сами бьют по чужим кораблям, а любой игрок может занять
///             орудие (см. <c>MannedCannon</c>).
///         </item>
///         <item>
///             Охрана — не пираты, а морские существа, которые уже есть в этом движке:
///             <c>WaterElemental</c>/<c>SeaSerpent</c>/<c>DeepSeaSerpent</c> (в ServUO ровно этот же
///             список <c>_SpawnTypes</c>).
///         </item>
///         <item>
///             <c>BeaconVulnerable</c> в ServUO зависел от живых расчётов пушек; здесь расчётов нет,
///             поэтому маяк уязвим всегда — <see cref="BeaconVulnerable" /> оставлен как точка
///             расширения и всегда true.
///         </item>
///         <item>
///             <c>OnSectorActivate</c>/<c>OnSectorDeactivate</c> у <c>BaseAddon</c> в этом движке нет:
///             таймер плота запускается при постановке в мир и отменяется в <c>OnAfterDelete</c>.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class PlunderBeaconAddon : BaseAddon
{
    /// <summary>Сколько охраны держится вокруг плота одновременно (в ServUO — <c>MaxSpawn</c>).</summary>
    public const int MaxSpawn = 5;

    /// <summary>Задержка перед первой волной охраны.</summary>
    private static readonly TimeSpan FirstSpawnDelay = TimeSpan.FromSeconds(30);

    /// <summary>Пределы паузы между появлением охраны (в ServUO — 30–60 секунд).</summary>
    private const int MinSpawnSeconds = 30;

    private const int MaxSpawnSeconds = 60;

    /// <summary>Как часто пушки плота ищут цель (в ServUO — <c>NextShoot</c> каждые 2 секунды).</summary>
    private static readonly TimeSpan ShootInterval = TimeSpan.FromSeconds(2);

    /// <summary>Шаг затопления разрушенного плота (в ServUO — 4 шага по 3 секунды).</summary>
    private static readonly TimeSpan SinkStep = TimeSpan.FromSeconds(3);

    private const int SinkStages = 4;

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PlunderBeacon _beacon;

    [Tidy]
    [SerializableField(1)]
    private List<Mobile> _attackers = [];

    [Tidy]
    [SerializableField(2)]
    private List<Item> _cannons = [];

    [SerializableField(3)]
    [AnchoredDateTime]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextSpawn;

    private TimerExecutionToken _tickToken;
    private TimerExecutionToken _sinkToken;
    private TimerExecutionToken _deleteToken;

    private DateTime _nextShoot;
    private int _sinkStage;

    /// <summary>Пустой конструктор — то, что ставит <c>[add</c> и спавнер.</summary>
    [Constructible]
    public PlunderBeaconAddon()
    {
        for (var i = 0; i < _simpleComponents.GetLength(0); i++)
        {
            AddComponent(
                new AddonComponent(_simpleComponents[i, 0]),
                _simpleComponents[i, 1],
                _simpleComponents[i, 2],
                _simpleComponents[i, 3]
            );
        }

        AddComplexComponent(2572, 0, 2, 37, 0, 5);
        AddComplexComponent(2567, 2, 0, 37, 0, 5);

        _beacon = new PlunderBeacon(this);
        _beacon.MoveToWorld(new Point3D(X + 1, Y + 1, Z + 14), Map);

        AddCannon(Direction.South, -3, 5, 7);
        AddCannon(Direction.South, 3, 5, 7);
        AddCannon(Direction.North, -3, -4, 7);
        AddCannon(Direction.North, 3, -4, 7);

        _nextSpawn = Core.Now + FirstSpawnDelay;
    }

    /// <summary>
    ///     В ServUO маяк был уязвим только когда на плоту не оставалось живых расчётов пушек. Здесь
    ///     расчётов нет, поэтому значение всегда true; свойство оставлено как точка расширения.
    /// </summary>
    public bool BeaconVulnerable => true;

    private void AddCannon(Direction facing, int xOffset, int yOffset, int zOffset)
    {
        var cannon = new MannedBlundercannon(null, facing)
        {
            CanFireUnmanned = true
        };

        cannon.MoveToWorld(new Point3D(X + xOffset, Y + yOffset, Z + zOffset), Map);

        _cannons.Add(cannon);
    }

    private void AddComplexComponent(int item, int xOffset, int yOffset, int zOffset, int hue, int lightSource)
    {
        var component = new AddonComponent(item);

        if (hue != 0)
        {
            component.Hue = hue;
        }

        if (lightSource != -1)
        {
            component.Light = (LightType)lightSource;
        }

        AddComponent(component, xOffset, yOffset, zOffset);
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        var delta = new Point3D(X - oldLocation.X, Y - oldLocation.Y, Z - oldLocation.Z);

        MoveWithPlatform(_beacon, delta);
        MoveWithPlatform(_cannons, delta);
        MoveWithPlatform(_attackers, delta);
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        if (_beacon?.Deleted == false)
        {
            _beacon.Map = Map;
        }

        SetMap(_cannons);
        SetMap(_attackers);

        if (Map == null || Map == Map.Internal)
        {
            _tickToken.Cancel();
            return;
        }

        PlunderBeaconSpawner.Register(this);
        StartTimer();
    }

    private void MoveWithPlatform(Item item, Point3D delta)
    {
        if (item?.Deleted == false)
        {
            item.Location = new Point3D(item.X + delta.X, item.Y + delta.Y, item.Z + delta.Z);
        }
    }

    private void MoveWithPlatform(List<Item> list, Point3D delta)
    {
        for (var i = 0; i < list.Count; i++)
        {
            MoveWithPlatform(list[i], delta);
        }
    }

    private void MoveWithPlatform(List<Mobile> list, Point3D delta)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var mobile = list[i];

            if (mobile?.Deleted == false)
            {
                mobile.Location = new Point3D(mobile.X + delta.X, mobile.Y + delta.Y, mobile.Z + delta.Z);
            }
        }
    }

    private void SetMap(List<Item> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i]?.Deleted == false)
            {
                list[i].Map = Map;
            }
        }
    }

    private void SetMap(List<Mobile> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i]?.Deleted == false)
            {
                list[i].Map = Map;
            }
        }
    }

    /// <summary>Запускает секундный такт плота: спавн охраны и стрельба пушек.</summary>
    private void StartTimer()
    {
        if (_tickToken.Running || Deleted)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick, out _tickToken);

        _nextShoot = Core.Now + ShootInterval;
    }

    private void OnTick()
    {
        if (Deleted || _beacon?.Deleted != false)
        {
            _tickToken.Cancel();
            return;
        }

        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        if (_nextSpawn <= Core.Now && CountAttackers() < MaxSpawn)
        {
            SpawnAttacker();

            _nextSpawn = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(MinSpawnSeconds, MaxSpawnSeconds));
        }

        if (Core.Now < _nextShoot)
        {
            return;
        }

        _nextShoot = Core.Now + ShootInterval;

        FireCannons();
    }

    private void FireCannons()
    {
        // Пушки сканируют линию огня по клеткам, поэтому без игрока рядом не стреляем вовсе:
        // иначе каждый плот тратил бы такт мира на пустой поиск цели.
        if (!AnyPlayerNearby())
        {
            return;
        }

        for (var i = 0; i < _cannons.Count; i++)
        {
            if (_cannons[i] is not MannedCannon cannon || cannon.Deleted)
            {
                continue;
            }

            var operatorMobile = cannon.Operator;

            if (!cannon.CanFireUnmanned && (operatorMobile?.Deleted != false || !operatorMobile.Alive))
            {
                continue;
            }

            cannon.Scan(true);
        }
    }

    private bool AnyPlayerNearby()
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        foreach (var player in map.GetMobilesInRange<PlayerMobile>(Location, 30))
        {
            if (!player.Deleted)
            {
                return true;
            }
        }

        return false;
    }

    private int CountAttackers()
    {
        var alive = 0;

        for (var i = _attackers.Count - 1; i >= 0; i--)
        {
            var attacker = _attackers[i];

            if (attacker?.Deleted != false)
            {
                _attackers.RemoveAt(i);
                continue;
            }

            alive++;
        }

        return alive;
    }

    /// <summary>Ставит одного охранника рядом с плотом (в ServUO — <c>SpawnHelper</c>).</summary>
    private void SpawnAttacker()
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        var center = Location;
        var range = 12;

        BaseCreature creature = Utility.Random(3) switch
        {
            0 => new WaterElemental(),
            1 => new SeaSerpent(),
            _ => new DeepSeaSerpent()
        };

        for (var i = 0; i < 50; i++)
        {
            var spawn = new Point3D(
                Utility.RandomMinMax(center.X - range, center.X + range),
                Utility.RandomMinMax(center.Y - range, center.Y + range),
                -5
            );

            if (!map.CanFit(spawn.X, spawn.Y, spawn.Z, 16, true, true, false))
            {
                continue;
            }

            creature.MoveToWorld(spawn, map);
            creature.Home = spawn;
            creature.RangeHome = 10;

            _attackers.Add(creature);
            return;
        }

        creature.Delete();
    }

    /// <summary>
    ///     Маяк разрушен: плот тонет в четыре шага и исчезает (в ServUO — <c>OnBeaconDestroyed</c>).
    /// </summary>
    public void OnBeaconDestroyed()
    {
        if (Deleted)
        {
            return;
        }

        _tickToken.Cancel();
        _sinkStage = 0;

        Timer.StartTimer(SinkStep, SinkStep, SinkStages, Sink, out _sinkToken);
    }

    private void Sink()
    {
        if (Deleted)
        {
            return;
        }

        Z -= 1;
        _sinkStage++;

        if (_sinkStage >= SinkStages)
        {
            Delete();
        }
    }

    public override void OnAfterDelete()
    {
        _tickToken.Cancel();
        _sinkToken.Cancel();
        _deleteToken.Cancel();

        if (_beacon?.Deleted == false)
        {
            _beacon.Delete();
        }

        _beacon = null;

        for (var i = 0; i < _cannons.Count; i++)
        {
            if (_cannons[i]?.Deleted == false)
            {
                _cannons[i].Delete();
            }
        }

        _cannons.Clear();

        for (var i = 0; i < _attackers.Count; i++)
        {
            if (_attackers[i]?.Deleted == false)
            {
                _attackers[i].Delete();
            }
        }

        _attackers.Clear();

        PlunderBeaconSpawner.Unregister(this);

        base.OnAfterDelete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _attackers ??= [];
        _cannons ??= [];

        if (_beacon?.Deleted != false)
        {
            Timer.StartTimer(Delete, out _deleteToken);
            return;
        }

        if (_nextSpawn == default)
        {
            _nextSpawn = Core.Now + FirstSpawnDelay;
        }

        if (Map == null || Map == Map.Internal)
        {
            return;
        }

        PlunderBeaconSpawner.Register(this);
        StartTimer();
    }
}
