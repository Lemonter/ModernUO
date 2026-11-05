/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/MaginciaPlants/MaginciaPlantSystem.cs).
///
///     Невидимый предмет-контроллер на карте: хранит задержку посадки для каждого игрока (отдельно для
///     Trammel и Felucca) и отвечает за проверки места под растение.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             ServUO создавал контроллеры прямо в <c>Initialize</c> — то есть до загрузки мира, из-за чего
///             после каждого рестарта появлялись дубликаты. Здесь, как и требует этот репозиторий, мир
///             дополняется идемпотентно после <c>ServerStarted</c> с проверкой наличия
///             (<c>Engines/ExploringTheDeep/Generate.cs</c>, <c>Engines/CityLoyalty/Setup.cs</c>).
///         </item>
///         <item>
///             Статические ссылки на Trammel/Felucca восстанавливаются после загрузки мира
///             (<c>[AfterDeserialization(false)]</c>), а не внутри <c>Deserialize</c>.
///         </item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />; <c>String.Format</c> → интерполяция в аргументе.</item>
///         <item>
///             <c>GetStaticTiles(x, y, true)</c> → <c>GetStaticAndMultiTiles(x, y)</c>: массива больше нет,
///             перечисление идёт <c>foreach</c> и завершается на первом же статике.
///         </item>
///         <item>
///             <c>ColUtility</c>/<c>List&lt;Mobile&gt;</c> при чистке таблицы заменены проходом по ключам
///             через <c>STArrayPool</c>-совместимый список — таблица обходится один раз за сохранение.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.NewMagincia;
using Server.Logging;

namespace Server.Engines.Plants;

[SerializationGenerator(0, false)]
public partial class MaginciaPlantSystem : Item
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(MaginciaPlantSystem));

    /// <summary>Задержка между посадками на острове, в днях.</summary>
    public static readonly int PlantDelay = 4;

    private static readonly Point3D _feluccaLocation = new(3715, 2049, 5);
    private static readonly Point3D _trammelLocation = new(3715, 2049, 5);

    [SerializableField(0, setter: "private")]
    private Dictionary<Mobile, DateTime> _plantDelayTable;

    [Constructible]
    public MaginciaPlantSystem() : base(3240)
    {
        Movable = false;
        _plantDelayTable = new Dictionary<Mobile, DateTime>();
    }

    public static bool Enabled { get; set; } = true;

    public static MaginciaPlantSystem FelInstance { get; private set; }

    public static MaginciaPlantSystem TramInstance { get; private set; }

    public static Rectangle2D[] MagGrowBounds { get; } =
    {
        new(3663, 2103, 19, 19),
        new(3727, 2199, 7, 7)
    };

    private static readonly Rectangle2D[] _noGrowZones =
    {
        new(3683, 2144, 21, 40),
        new(3682, 2189, 39, 44),
        new(3654, 2233, 23, 30),
        new(3727, 2217, 15, 45),
        new(3558, 2134, 8, 8),
        new(3679, 2018, 70, 28)
    };

    public static void Initialize() => EventSink.ServerStarted += OnServerStarted;

    private static void OnServerStarted()
    {
        if (!Enabled)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(20), EnsureSetup);
    }

    private static void EnsureSetup()
    {
        try
        {
            var created = 0;

            if (FelInstance == null)
            {
                FelInstance = FindAt(_feluccaLocation, Map.Felucca);

                if (FelInstance == null)
                {
                    FelInstance = new MaginciaPlantSystem();
                    FelInstance.MoveToWorld(_feluccaLocation, Map.Felucca);
                    created++;
                }
            }

            if (TramInstance == null)
            {
                TramInstance = FindAt(_trammelLocation, Map.Trammel);

                if (TramInstance == null)
                {
                    TramInstance = new MaginciaPlantSystem();
                    TramInstance.MoveToWorld(_trammelLocation, Map.Trammel);
                    created++;
                }
            }

            if (created > 0)
            {
                _logger.Information("New Magincia plant system is set up ({Count} controllers placed)", created);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "New Magincia plant system setup failed");
        }
    }

    private static MaginciaPlantSystem FindAt(Point3D location, Map map)
    {
        foreach (var item in map.GetItemsInRange(location, 2))
        {
            if (item is MaginciaPlantSystem system)
            {
                return system;
            }
        }

        return null;
    }

    public bool CheckPlantDelay(Mobile from)
    {
        if (_plantDelayTable.TryGetValue(from, out var endsAt) && endsAt > Core.Now)
        {
            var left = endsAt - Core.Now;

            // Time remaining to plant on the Isle of Magincia again: ~1_val~ days ~2_val~ hours ~3_val~ minutes.
            from.SendLocalizedMessage(1150459, $"{left.Days}\t{left.Hours}\t{left.Minutes}");
            return false;
        }

        return true;
    }

    public void OnPlantDelete(Mobile from) => _plantDelayTable.Remove(from);

    public void OnPlantPlanted(Mobile from)
    {
        if (from.AccessLevel == AccessLevel.Player)
        {
            _plantDelayTable[from] = Core.Now + TimeSpan.FromDays(PlantDelay);
        }
        else
        {
            from.SendMessage($"Как сотрудник вы обходите задержку посадки в {PlantDelay} дн.");
        }
    }

    /// <summary>Контроллер неудаляем: без него перестанут работать проверки посадки.</summary>
    public override void Delete()
    {
    }

    public static bool CanAddPlant(Mobile from, Point3D p)
    {
        if (!IsValidLocation(p))
        {
            from.SendLocalizedMessage(1150457); // The ground here is not good for gardening.
            return false;
        }

        var map = from.Map;

        if (map == null)
        {
            return false;
        }

        var plantCount = 0;

        foreach (var item in map.GetItemsInRange(p, 17))
        {
            if (item is MaginciaPlantItem)
            {
                if (item.Location != p)
                {
                    plantCount++;
                    continue;
                }

                from.SendLocalizedMessage(1150367); // This plot already has a plant!
                return false;
            }

            if (!item.Movable && item.Location == p)
            {
                from.SendLocalizedMessage(1150457); // The ground here is not good for gardening.
                return false;
            }
        }

        if (plantCount > 34)
        {
            // There are too many objects in this area to plant (limit 34 per 17x17 area).
            from.SendLocalizedMessage(1150491);
            return false;
        }

        foreach (var _ in map.Tiles.GetStaticAndMultiTiles(p.X, p.Y))
        {
            from.SendLocalizedMessage(1150457); // The ground here is not good for gardening.
            return false;
        }

        return true;
    }

    public static bool CheckDelay(Mobile from)
    {
        MaginciaPlantSystem system = null;

        if (from.Map == Map.Trammel)
        {
            system = TramInstance;
        }
        else if (from.Map == Map.Felucca)
        {
            system = FelInstance;
        }

        if (system == null)
        {
            from.SendLocalizedMessage(1150457); // The ground here is not good for gardening.
            return false;
        }

        return system.CheckPlantDelay(from);
    }

    public static bool IsValidLocation(Point3D p)
    {
        foreach (var zone in _noGrowZones)
        {
            if (zone.Contains(p))
            {
                return false;
            }
        }

        foreach (var zone in MaginciaHousingZones.Zones)
        {
            var expanded = new Rectangle2D(zone.X - 2, zone.Y - 2, zone.Width + 4, zone.Height + 7);

            if (expanded.Contains(p))
            {
                return false;
            }
        }

        return true;
    }

    public static void OnPlantDelete(Mobile owner, Map map)
    {
        if (owner == null || map == null)
        {
            return;
        }

        if (map == Map.Trammel)
        {
            TramInstance?.OnPlantDelete(owner);
        }
        else if (map == Map.Felucca)
        {
            FelInstance?.OnPlantDelete(owner);
        }
    }

    public static void OnPlantPlanted(Mobile from, Map map)
    {
        if (map == Map.Felucca)
        {
            FelInstance?.OnPlantPlanted(from);
        }
        else if (map == Map.Trammel)
        {
            TramInstance?.OnPlantPlanted(from);
        }
    }

    public void DefragPlantDelayTable()
    {
        List<Mobile> expired = null;

        foreach (var (mobile, expires) in _plantDelayTable)
        {
            if (expires < Core.Now)
            {
                (expired ??= new List<Mobile>()).Add(mobile);
            }
        }

        if (expired == null)
        {
            return;
        }

        foreach (var mobile in expired)
        {
            _plantDelayTable.Remove(mobile);
        }
    }

    [AfterDeserialization(false)]
    private void OnAfterDeserialization()
    {
        if (Map == Map.Felucca)
        {
            FelInstance = this;
        }
        else if (Map == Map.Trammel)
        {
            TramInstance = this;
        }
    }
}
