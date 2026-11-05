using System;
using System.Collections.Generic;
using System.IO;
using ModernUO.Serialization;
using Server.Engines.Harvest;
using Server.Logging;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Covetous void pool (Items/HarvestMap.cs) — a consumable map that points at a
///     random mining or lumberjacking spot of its resource and burns a use when you dig there.
///
///     Location tables ship with the item: ServUO's <c>Data/HarvestLocs/*.cfg</c> (twelve files, one per
///     map and gathering type) are copied into <c>Distribution/Data/HarvestLocs</c> and read lazily, per
///     map, the first time a map of that kind is rolled.
///
///     Adapted: the 10-second decay poll is a single timer for the remaining lifespan; a missing data
///     file logs through LogFactory and leaves the map without a target instead of writing to the console;
///     the harvest hook that spends uses (<c>CheckMapOnHarvest</c>) is not wired into the gathering
///     systems yet, so the map works as a locator only for now.
/// </summary>
[SerializationGenerator(0, false)]
public partial class HarvestMap : Item, IUsesRemaining
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(HarvestMap));

    public const int DecayPeriod = 24;

    private static readonly Dictionary<Map, List<Point2D>> MinerLocs = new();
    private static readonly Dictionary<Map, List<Point2D>> LumberLocs = new();

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public CraftResource Resource
    {
        get => _resource;
        set
        {
            if (_resource == value)
            {
                return;
            }

            _resource = value;
            Hue = CraftResources.GetHue(value);
            InvalidateProperties();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int UsesRemaining
    {
        get => _usesRemaining;
        set
        {
            _usesRemaining = value;

            if (_usesRemaining <= 0 && RootParent is Mobile parent)
            {
                parent.SendMessage("Магия вашей карты исчерпана.");
            }

            InvalidateProperties();
        }
    }

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _showUsesRemaining;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point2D _target;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _targetMap;

    private TimerExecutionToken _decayToken;

    public bool IsMinerMap => _resource >= CraftResource.Iron && _resource <= CraftResource.Valorite;

    /// <summary>
    ///     Ищет в рюкзаке игрока карту, которая покрывает точку добычи: та же карта мира и та же жила.
    ///     Если нашлась — добыча даст ресурс карты, а карта потратит одно использование.
    /// </summary>
    public static HarvestMap CheckMapOnHarvest(Mobile from, Point3D loc, HarvestDefinition def)
    {
        var pack = from?.Backpack;

        if (pack == null || def == null)
        {
            return null;
        }

        var map = from.Map;

        foreach (var item in pack.Items)
        {
            if (item is not HarvestMap harvestMap || harvestMap.UsesRemaining <= 0)
            {
                continue;
            }

            if (harvestMap._targetMap != map)
            {
                continue;
            }

            if (def.GetBank(map, loc.X, loc.Y) == def.GetBank(harvestMap._targetMap, harvestMap._target.X, harvestMap._target.Y))
            {
                return harvestMap;
            }
        }

        return null;
    }

    [Constructible]
    public HarvestMap() : this(CraftResource.Iron)
    {
    }

    public HarvestMap(CraftResource resource) : base(0x14EC)
    {
        Resource = resource;
        GetRandomLocation();

        UsesRemaining = Utility.RandomMinMax(235, 255);
        _showUsesRemaining = true;

        _expires = Core.Now + TimeSpan.FromHours(DecayPeriod);
        ScheduleDecay();
    }

    private void ScheduleDecay()
    {
        _decayToken.Cancel();

        var remaining = _expires - Core.Now;

        if (remaining <= TimeSpan.Zero)
        {
            Timer.DelayCall(Decay);
            return;
        }

        Timer.StartTimer(remaining, Decay, out _decayToken);
    }

    public void Decay()
    {
        if (RootParent is Mobile parent)
        {
            parent.SendLocalizedMessage(1072515, Name == null ? $"#{LabelNumber}" : Name); // The ~1_name~ expired...
            Effects.SendLocationParticles(
                EffectItem.Create(parent.Location, parent.Map, EffectItem.DefaultDuration),
                0x3728,
                8,
                20,
                5042
            );
            Effects.PlaySound(parent.Location, parent.Map, 0x201);
        }
        else
        {
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                8,
                20,
                5042
            );
            Effects.PlaySound(Location, Map, 0x201);
        }

        Delete();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _decayToken.Cancel();
    }

    public override void AddNameProperty(IPropertyList list)
    {
        // ~1_RES~ ~2_TYPE~ Map
        list.Add(1152598, $"#{CraftResources.GetLocalizationNumber(Resource)}\t#{(IsMinerMap ? "1152604" : "1152605")}");
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_usesRemaining > 0)
        {
            list.Add(1060584, _usesRemaining.ToString()); // uses remaining: ~1_val~
        }

        var left = _expires > Core.Now ? (int)(_expires - Core.Now).TotalSeconds : 0;
        list.Add(1072517, left.ToString()); // Lifespan: ~1_val~ seconds
        list.Add(1061114, GetSextantLocation()); // Location: ~1_val~

        if (_targetMap == null || _targetMap == Map.Internal)
        {
            list.Add(1060800); // unknown
        }
        else
        {
            list.Add(_targetMap.MapID + 1150548); // map name
        }
    }

    private string GetSextantLocation()
    {
        if (_targetMap == null)
        {
            return "Unknown";
        }

        int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
        var xEast = false;
        var ySouth = false;

        var point = new Point3D(_target.X, _target.Y, _targetMap.GetAverageZ(_target.X, _target.Y));

        if (Sextant.Format(point, _targetMap, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
        {
            return $"{yLat}° {yMins}'{(ySouth ? "S" : "N")}, {xLong}° {xMins}'{(xEast ? "E" : "W")}";
        }

        return "Unknown";
    }

    public void GetRandomLocation()
    {
        var map = Utility.Random(6) switch
        {
            0 => Map.Felucca,
            1 => Map.Trammel,
            2 => Map.Ilshenar,
            3 => Map.Malas,
            4 => Map.Tokuno,
            _ => Map.TerMur
        };

        _targetMap = map;

        var table = IsMinerMap ? MinerLocs : LumberLocs;

        if (!table.TryGetValue(map, out var locations) || locations == null || locations.Count == 0)
        {
            locations = LoadLocsFor(map);

            if (locations == null)
            {
                logger.Warning("No harvest locations for {Map} ({Resource}); the map has no target", map, Resource);
                return;
            }

            table[map] = locations;
        }

        _target = locations[Utility.Random(locations.Count)];
    }

    private List<Point2D> LoadLocsFor(Map map)
    {
        var name = IsMinerMap ? "MinerLocs" : "LumberLocs";
        var path = Path.Combine(Core.BaseDirectory, "Data", "HarvestLocs", $"{name}_{map}.cfg");

        if (!File.Exists(path))
        {
            logger.Warning("Harvest map data file {Path} is missing", path);
            return null;
        }

        var list = new List<Point2D>();

        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var split = line.Split('\t');

            if (split.Length >= 2 && int.TryParse(split[0], out var x) && int.TryParse(split[1], out var y) &&
                (x > 0 || y > 0))
            {
                list.Add(new Point2D(x, y));
            }
        }

        return list;
    }

    /// <summary>Finds a harvest map in the digger's pack that matches the spot being gathered, so the
    /// caller can spend one of its uses. Not yet called from the gathering systems — see the class note.</summary>
    public static HarvestMap CheckMapOnHarvest(Mobile from, object harvested, HarvestDefinition def)
    {
        if (harvested is not IPoint3D point || from.Backpack == null)
        {
            return null;
        }

        var bank = def.GetBank(from.Map, point.X, point.Y);

        return from.Backpack.FindItemByType<HarvestMap>(
            true,
            map => map.TargetMap == from.Map && map.UsesRemaining > 0 &&
                   def.GetBank(map.TargetMap, map.Target.X, map.Target.Y) == bank
        );
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_expires <= Core.Now)
        {
            Timer.DelayCall(Decay);
        }
        else
        {
            ScheduleDecay();
        }
    }
}
