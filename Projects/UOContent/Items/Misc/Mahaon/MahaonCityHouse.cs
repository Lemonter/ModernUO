using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     One "apartment" inside an existing city building — a hand-marked SET of individual
///     floor tiles (not a rectangle: real city buildings are irregular shapes and often
///     multiple stories, so "two corners" doesn't describe their footprint). Plus its own
///     owner/friends/bans, entirely separate from BaseHouse (which assumes it owns a
///     placeable Multi; these buildings are permanent map architecture, not something a
///     player's house deed could ever place).
/// </summary>
[SerializationGenerator(1, false)]
public partial class MahaonCityHouse : Item
{
    // How close a player's actual Z needs to be to a marked tile's Z to count as "standing
    // on it" — small slack for stairs/uneven terrain under a single floor, but tight enough
    // that a tile marked on the ground floor and the exact same X/Y one floor up (a common
    // shape for a multi-story building) are treated as genuinely different tiles.
    private const int ZTolerance = 3;

    [SerializableField(0)]
    private Map _areaMap;

    [SerializableField(1)]
    private List<Point3D> _tiles;

    [SerializableField(2)]
    private string _label;

    [SerializableField(3)]
    private Mobile _owner;

    [SerializableField(4)]
    private List<Mobile> _friends;

    [SerializableField(5)]
    private List<Mobile> _bans;

    [SerializableField(6)]
    private List<uint> _furnitureOverrideIds;

    // Purchase price; 0 means priced by floor area (MahaonCityHouseSystem.PricePerTile).
    [SerializableField(7)]
    private int _price;

    // Items fixed in place, and containers locked for the household.
    [SerializableField(8)]
    private List<Item> _lockdowns = [];

    [SerializableField(9)]
    private List<Item> _secures = [];

    private void MigrateFrom(V0Content content)
    {
        _areaMap = content.AreaMap;
        _tiles = content.Tiles ?? [];
        _label = content.Label;
        _owner = content.Owner;
        _friends = content.Friends ?? [];
        _bans = content.Bans ?? [];
        _furnitureOverrideIds = content.FurnitureOverrideIds ?? [];
        _lockdowns = [];
        _secures = [];
    }

    private Systems.MahaonWorld.MahaonCityHouseRegion _region;

    public Systems.MahaonWorld.MahaonCityHouseRegion Region => _region;

    /// <summary>What the house costs to buy.</summary>
    public int SalePrice => _price > 0 ? _price : Tiles.Count * Systems.MahaonWorld.MahaonCityHouseSystem.PricePerTile;

    /// <summary>Rebuilds the house's region from its tiles — on load, and whenever the tiles change.</summary>
    public void UpdateRegion()
    {
        _region?.Unregister();
        _region = null;

        if (AreaMap != null && Tiles.Count > 0 && !Deleted)
        {
            _region = new Systems.MahaonWorld.MahaonCityHouseRegion(this);
            _region.Register();
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Timer.DelayCall(UpdateRegion);

    [Constructible]
    public MahaonCityHouse(Map map, List<Point3D> tiles, string label) : base(0x1F14)
    {
        _areaMap = map;
        _tiles = new List<Point3D>(tiles);
        _label = label;
        _friends = new List<Mobile>();
        _bans = new List<Mobile>();
        _furnitureOverrideIds = new List<uint>();

        Movable = false;
        Visible = false; // real persisted Item, purely an object reference — same as MahaonMarkerArea
    }

    /// <summary>Is this exact spot (X/Y and close-enough Z) one of the marked floor tiles —
    /// used for "is the player actually standing inside this house" checks (friend-only
    /// perks, etc., once those exist).</summary>
    public bool Contains(Point3D loc, Map map)
    {
        if (map != AreaMap)
        {
            return false;
        }

        foreach (var tile in Tiles)
        {
            if (tile.X == loc.X && tile.Y == loc.Y && (tile.Z - loc.Z).Abs() <= ZTolerance)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>X/Y-only membership, ignoring Z and floor — used by the movement-passability
    /// hook, which only ever asks "is there a marked tile anywhere in this column" (see
    /// MahaonCityHouseSystem.IsInsideAnyHouse). A two-story house sharing the same X/Y on
    /// both floors is a real, common case; passability doesn't need to tell them apart the
    /// way furniture-hiding does.</summary>
    public bool ContainsColumn(int x, int y)
    {
        foreach (var tile in Tiles)
        {
            if (tile.X == x && tile.Y == y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A marked tile in this column within <paramref name="zRange"/> of <paramref name="z"/>.</summary>
    public bool HasTileNear(int x, int y, int z, int zRange)
    {
        foreach (var tile in Tiles)
        {
            if (tile.X == x && tile.Y == y && (tile.Z - z).Abs() <= zRange)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How many items the house may hold fixed: three per tile of floor.</summary>
    public int MaxLockdowns => Tiles.Count * 3;

    public int LockdownCount
    {
        get
        {
            Lockdowns.RemoveAll(i => i?.Deleted != false);
            Secures.RemoveAll(i => i?.Deleted != false);
            return Lockdowns.Count + Secures.Count;
        }
    }

    public bool IsLockedDown(Item item) => Lockdowns.Contains(item) || Secures.Contains(item);

    /// <summary>Fixes an item on the floor in place, or locks a container for the household.
    /// Returns a message either way.</summary>
    public string LockDown(Mobile m, Item item, bool secure)
    {
        if (!IsOwner(m))
        {
            return "Закреплять вещи может только хозяин.";
        }

        if (item.Parent != null || !Contains(item.Location, item.Map))
        {
            return "Вещь должна лежать на полу внутри дома.";
        }

        if (!item.Movable || IsLockedDown(item))
        {
            return "Это уже закреплено.";
        }

        if (secure && item is not Container)
        {
            return "Под замок можно поставить только сундук или ящик.";
        }

        if (LockdownCount >= MaxLockdowns)
        {
            return $"В доме уже закреплено всё, что можно: {MaxLockdowns}.";
        }

        item.Movable = false;

        if (secure)
        {
            item.IsSecure = true;
            Secures.Add(item);
        }
        else
        {
            item.IsLockedDown = true;
            Lockdowns.Add(item);
        }

        this.MarkDirty();
        return secure ? "Сундук под замком: открыть его смогут хозяин и друзья." : "Вещь закреплена.";
    }

    public string Release(Mobile m, Item item)
    {
        if (!IsOwner(m))
        {
            return "Освобождать вещи может только хозяин.";
        }

        if (!IsLockedDown(item))
        {
            return "Это не закреплено.";
        }

        ReleaseItem(item);
        return "Вещь освобождена.";
    }

    private void ReleaseItem(Item item)
    {
        Lockdowns.Remove(item);
        Secures.Remove(item);
        item.IsLockedDown = false;
        item.IsSecure = false;
        item.Movable = true;
        this.MarkDirty();
    }

    /// <summary>Lets go of everything fixed in the house — when it changes hands or is given up.</summary>
    public void ReleaseAll()
    {
        foreach (var item in new List<Item>(Lockdowns))
        {
            if (item?.Deleted == false)
            {
                ReleaseItem(item);
            }
        }

        foreach (var item in new List<Item>(Secures))
        {
            if (item?.Deleted == false)
            {
                ReleaseItem(item);
            }
        }

        Lockdowns.Clear();
        Secures.Clear();
    }

    /// <summary>Whether <paramref name="m"/> may open or use a fixed item of the house.</summary>
    public bool CanAccess(Mobile m, Item item) => !IsLockedDown(item) || IsFriend(m);

    public bool IsOwner(Mobile m) => m != null && (m == Owner || m.AccessLevel >= AccessLevel.GameMaster);

    public bool IsFriend(Mobile m) => m != null && (IsOwner(m) || Friends.Contains(m));

    public bool IsBanned(Mobile m) => m != null && Bans.Contains(m);

    public override void OnDelete()
    {
        ReleaseAll();
        RemoveFurnitureHiding();
        _region?.Unregister();
        _region = null;
        Systems.MahaonWorld.MahaonCityHouseSystem.Unregister(this);
        base.OnDelete();
    }

    // A century — effectively "forever, until we say otherwise" for
    // StaticOverrideManager's timer-based expiry.
    private static readonly System.TimeSpan FurnitureHideDuration = System.TimeSpan.FromDays(365 * 100);

    /// <summary>Sends a "draw nothing here" override for every static tile found at each
    /// marked (X, Y) whose Z is close to that tile's own marked Z — literally everything on
    /// that floor at that spot, no per-graphic allowlist, per the original request ("вся
    /// статика внутри отмеченных плиток автоматически"). The Z check specifically avoids
    /// also hiding a completely different floor's furniture stacked at the same X/Y in a
    /// multi-story building. Safe to call again (e.g. after a restart, since
    /// StaticOverrideManager itself doesn't persist) — old override IDs are cleared first.</summary>
    public void ApplyFurnitureHiding()
    {
        RemoveFurnitureHiding();

        if (AreaMap == null)
        {
            return;
        }

        foreach (var marked in Tiles)
        {
            foreach (var tile in AreaMap.Tiles.GetStaticTiles(marked.X, marked.Y))
            {
                if (System.Math.Abs(tile.Z - marked.Z) > ZTolerance)
                {
                    continue; // a different floor's furniture stacked at the same X/Y
                }

                var loc = new Point3D(marked.X, marked.Y, tile.Z);

                var id = Systems.MahaonWorld.StaticOverrideManager.AddOverride(
                    loc, AreaMap, (ushort)tile.ID, 0, FurnitureHideDuration
                );

                FurnitureOverrideIds.Add(id);
            }
        }
    }

    public void RemoveFurnitureHiding()
    {
        foreach (var id in FurnitureOverrideIds)
        {
            Systems.MahaonWorld.StaticOverrideManager.RemoveOverride(id);
        }

        FurnitureOverrideIds.Clear();
    }
}
