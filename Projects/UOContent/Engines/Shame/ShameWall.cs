using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.ShameRevamped;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (ShameWall.cs) — the rubble wall that blocks a passage
///     until its cave troll dies, plus the pass-through teleporters that only work while the wall is up.
///
///     ServUO's multi-version Deserialize (save-compat migrations for shards that had the dungeon from an
///     earlier revision) is not carried over: this is a fresh port with no existing save data.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShameWall : BaseAddon
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CaveTroll _troll;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _startSpot;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _startMap;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _trollSpawnLoc;

    private static readonly Point3D[][] TeleportLocs =
    {
        new[] { new Point3D(5402, 82, 10), new Point3D(5402, 81, 10), new Point3D(5402, 83, 10) },
        new[] { new Point3D(5403, 82, 10), new Point3D(5403, 81, 10), new Point3D(5403, 83, 10) },
        new[] { new Point3D(5404, 82, 10), new Point3D(5404, 81, 10), new Point3D(5404, 83, 10) },
        new[] { new Point3D(5405, 82, 10), new Point3D(5405, 81, 10), new Point3D(5405, 83, 10) },

        new[] { new Point3D(5465, 25, -10), new Point3D(5464, 25, -10), new Point3D(5466, 25, -10) },
        new[] { new Point3D(5465, 26, -10), new Point3D(5464, 26, -10), new Point3D(5466, 26, -10) },
        new[] { new Point3D(5465, 27, -10), new Point3D(5464, 27, -10), new Point3D(5466, 27, -10) },
        new[] { new Point3D(5465, 28, -10), new Point3D(5464, 28, -10), new Point3D(5466, 28, -10) },

        new[] { new Point3D(5618, 57, 0), new Point3D(5618, 58, 0), new Point3D(5618, 56, 0) },
        new[] { new Point3D(5619, 57, 0), new Point3D(5619, 58, 0), new Point3D(5619, 56, 0) },
        new[] { new Point3D(5620, 57, 0), new Point3D(5620, 58, 0), new Point3D(5620, 56, 0) }
    };

    public ShameWall(Dictionary<Point3D, int> details, Point3D startSpot, Point3D trollSpawnLoc, Map map)
    {
        foreach (var (offset, itemId) in details)
        {
            AddComponent(new AddonComponent(itemId), offset.X, offset.Y, offset.Z);
        }

        _startSpot = startSpot;
        _startMap = map;
        _trollSpawnLoc = trollSpawnLoc;

        SpawnTroll();
    }

    public void OnTrollKilled()
    {
        Z -= 50;
        Visible = false;

        Timer.DelayCall(TimeSpan.FromMinutes(2), Reset);

        _troll = null;
    }

    public void Reset()
    {
        MoveToWorld(_startSpot, _startMap);
        Visible = true;
        SpawnTroll();
    }

    private void SpawnTroll()
    {
        if (_troll is { Deleted: false, Alive: true })
        {
            return;
        }

        _troll = new CaveTroll(this);
        _troll.MoveToWorld(_trollSpawnLoc, _startMap);

        _troll.Home = _trollSpawnLoc;
        _troll.RangeHome = 8;
    }

    public override void OnComponentUsed(AddonComponent component, Mobile from)
    {
        base.OnComponentUsed(component, from);

        if (_troll is not { Deleted: false, Alive: true })
        {
            Reset();
        }
    }

    public static void AddTeleporters(ShameWall wall)
    {
        var map = wall.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        foreach (var component in wall.Components)
        {
            foreach (var points in TeleportLocs)
            {
                if (component.Location != points[0])
                {
                    continue;
                }

                foreach (var item in map.GetItemsInRange(points[1], 0))
                {
                    if (item is ConditionTeleporter)
                    {
                        WeakEntityCollection.Remove("newshame", item);
                        item.Delete();
                    }
                }

                var teleporter = new ShameWallTeleporter(points[2], map);
                teleporter.MoveToWorld(points[1], map);

                WeakEntityCollection.Add("newshame", teleporter);
            }
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_troll != null)
        {
            _troll.Wall = this;
        }

        if (Location != _startSpot || _troll is not { Deleted: false, Alive: true })
        {
            Reset();
        }
    }
}
