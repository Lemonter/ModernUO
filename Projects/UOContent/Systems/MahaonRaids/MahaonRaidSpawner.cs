using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
// ReSharper disable once RedundantUsingDirective — GetDistanceToSqrt (Utility.cs, namespace
// Server) is an extension method; despite Server.Items nesting under Server, extension
// method lookup didn't pick it up without this being explicit.
using Server;

namespace Server.Items;

/// <summary>
///     The "special block, spawn everything at once" requested separately from the normal
///     Spawner — a GM places one of these at the spawn point, sets the mob type name and
///     count, then double-clicks to trigger. Finds the nearest MahaonRaidMarker to know
///     which city the raiders should head for (sets their Home there, same pattern the
///     automatic RaidEventSystem already uses) and spawns a batch of MahaonTownDefenders
///     there too so guards actually have someone to fight. Raiders/defenders exchange only
///     1-2 damage either way (MahaonRaidCombat) — this is a spectacle for farming guard
///     rank, not a real threat.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonRaidSpawner : Item
{
    [SerializableField(0)]
    private string _mobTypeName = "RaidBandit";

    [SerializableField(1)]
    private int _minCount = 6;

    [SerializableField(2)]
    private int _maxCount = 10;

    [SerializableField(3)]
    private int _defenderCount = 4;

    [SerializableField(4)]
    private int _markerSearchRange = 64;

    [Constructible]
    public MahaonRaidSpawner() : base(0x1F14) // campfire-ish graphic, purely a GM tool
    {
        Movable = false;
        Visible = false;
        Name = "Спаунер набега";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        var type = AssemblyHandler.FindTypeByName(_mobTypeName);

        if (type == null || !typeof(BaseCreature).IsAssignableFrom(type))
        {
            from.SendMessage(0x22, $"Неизвестный тип моба: {_mobTypeName}");
            return;
        }

        var (raiderCount, cityLabel) = TriggerSpawn();

        from.SendMessage(0x59, $"Набег запущен: {raiderCount} врагов, {_defenderCount} защитников у {cityLabel}.");
    }

    /// <summary>The actual spawn logic, split out from OnDoubleClick so
    /// Systems.MahaonQuests.MahaonVersaSystem can trigger a raid on its own decision —
    /// no player double-clicked anything, so there's no "from" Mobile to permission-check
    /// against; this is a system-level trigger, same spawn code either way. Returns how
    /// many raiders actually got created and a label for whatever called this to use in
    /// its own message (marker's city name if one was found, generic fallback otherwise).</summary>
    public (int raiderCount, string cityLabel) TriggerSpawn()
    {
        var type = AssemblyHandler.FindTypeByName(_mobTypeName);

        if (type == null || !typeof(BaseCreature).IsAssignableFrom(type))
        {
            return (0, "точки спауна");
        }

        var marker = FindNearestMarker();
        var targetLoc = marker?.Location ?? Location;
        var targetMap = marker?.Map ?? Map;

        var count = Utility.RandomMinMax(_minCount, _maxCount);
        var raiders = new List<BaseCreature>();

        for (var i = 0; i < count; i++)
        {
            if (Activator.CreateInstance(type) is not BaseCreature creature)
            {
                continue;
            }

            var spawnLoc = FindNearbySpawnPoint(Location, Map, 8);
            creature.MoveToWorld(spawnLoc, Map);
            creature.Home = targetLoc;
            creature.RangeHome = 10;
            raiders.Add(creature);
        }

        for (var i = 0; i < _defenderCount; i++)
        {
            BaseCreature defender = i % 3 == 2 ? new MahaonTownDefenderMage() : new MahaonTownDefender();
            var spawnLoc = FindNearbySpawnPoint(targetLoc, targetMap, 6);
            defender.MoveToWorld(spawnLoc, targetMap);
            defender.Home = targetLoc;
            defender.RangeHome = 10;
        }

        // "Burning" — a real building-static swap (like the tree-stump override system)
        // needs the actual graphic IDs of whatever counts as "a building" here, which
        // varies by city and I don't have; scattering a few fire effects near the target
        // is a much safer generic stand-in that doesn't risk guessing wrong. Ask for
        // specific building graphic IDs (same process as the tree IDs earlier) if a real
        // static swap is wanted instead.
        for (var i = 0; i < 4; i++)
        {
            var fireLoc = FindNearbySpawnPoint(targetLoc, targetMap, 6);
            Effects.SendLocationEffect(fireLoc, targetMap, 0x3E23, 16, 10);
        }

        var cityLabel = marker != null ? marker.CityName : "точки спауна";
        if (raiders.Count > 0)
        {
            Systems.MahaonRaids.RaidAlarm.Raise(targetMap, targetLoc, cityLabel);
        }

        return (raiders.Count, cityLabel);
    }

    private MahaonRaidMarker FindNearestMarker()
    {
        if (Map == null)
        {
            return null;
        }

        MahaonRaidMarker closest = null;
        var closestDist = int.MaxValue;

        foreach (var item in Map.GetItemsInRange<MahaonRaidMarker>(Location, _markerSearchRange))
        {
            var dist = (int)Utility.GetDistanceToSqrt(this, item.Location);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = item;
            }
        }

        return closest;
    }

    private static Point3D FindNearbySpawnPoint(Point3D center, Map map, int radius)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var x = center.X + Utility.RandomMinMax(-radius, radius);
            var y = center.Y + Utility.RandomMinMax(-radius, radius);
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanSpawnMobile(candidate))
            {
                return candidate;
            }
        }

        return center;
    }
}
