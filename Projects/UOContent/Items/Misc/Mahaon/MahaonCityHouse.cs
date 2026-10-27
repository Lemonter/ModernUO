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

    // Furniture placed from deeds: forges, looms, beds.
    [SerializableField(10)]
    private List<Item> _addons = [];

    // The cellar bought under the house: its floor, walls and the two hatches.
    [SerializableField(11)]
    private List<Item> _basement = [];

    [SerializableField(12)]
    private bool _hasBasement;

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
        _addons = [];
        _basement = [];
    }

    private Systems.MahaonWorld.MahaonCityHouseRegion _region;

    public Systems.MahaonWorld.MahaonCityHouseRegion Region => _region;

    /// <summary>What the house costs to buy.</summary>
    public int SalePrice => _price > 0 ? _price : Tiles.Count * Systems.MahaonWorld.MahaonCityHouseSystem.PricePerTile;

    // ---- Basement --------------------------------------------------------------------------

    /// <summary>How far below the ground floor the cellar lies.</summary>
    public const int BasementDepth = 50;

    private const int LowestZ = -120;
    private const int BasementFloorId = 0x0515; // cobblestones, as the town roads are laid

    /// <summary>The ground floor: the tiles at the house's lowest level.</summary>
    public List<Point3D> GroundTiles()
    {
        var minZ = int.MaxValue;
        foreach (var t in Tiles)
        {
            minZ = System.Math.Min(minZ, t.Z);
        }

        var ground = new List<Point3D>();
        foreach (var t in Tiles)
        {
            if (t.Z - minZ <= ZTolerance)
            {
                ground.Add(t);
            }
        }

        return ground;
    }

    /// <summary>A cellar costs half what the ground floor above it does.</summary>
    public int BasementPrice => GroundTiles().Count * Systems.MahaonWorld.MahaonCityHouseSystem.PricePerTile / 2;

    // Люк в полу первого этажа (корабельный люк — поверхность, по нему ходят) и каменные
    // ступени выхода в углу подвала.
    private const int HatchGraphic = 0x3EAE;
    private const int ExitStepsGraphic = 0x079E;

    // Тонкая каменная стена — те же детали, что у старой ограды: «\» идёт вдоль X, «/» вдоль Y.
    private const int WallAlongX = 0x001C;
    private const int WallAlongY = 0x001B;

    /// <summary>
    /// Digs the cellar: a floor under the whole ground floor <see cref="BasementDepth"/> Z down,
    /// walls along its north and west sides, a hatch in the ground floor and steps back up in a
    /// corner. The cellar's tiles join the house, so its region, lockdowns and furniture work
    /// down there too.
    /// </summary>
    public bool BuildBasement()
    {
        if (HasBasement || AreaMap == null)
        {
            return false;
        }

        var ground = GroundTiles();
        if (ground.Count == 0)
        {
            return false;
        }

        var groundZ = ground[0].Z;
        var z = System.Math.Max(LowestZ, groundZ - BasementDepth);
        var columns = new HashSet<Point2D>();
        var cellar = new List<Point3D>();

        foreach (var t in ground)
        {
            columns.Add(new Point2D(t.X, t.Y));
            var floor = new Static(BasementFloorId);
            floor.MoveToWorld(new Point3D(t.X, t.Y, z), AreaMap);
            Basement.Add(floor);
            cellar.Add(new Point3D(t.X, t.Y, z));
        }

        BuildFixtures(ground, columns, z);

        HasBasement = true;
        AddTiles(cellar);
        return true;
    }

    /// <summary>
    /// Стены, люк и ступени подвала поверх готового пола.
    ///
    /// Стены — только северная и западная. Каменная стена UO стоит на ЮЖНОМ или ВОСТОЧНОМ
    /// ребре своей клетки, поэтому северную стену пола несут клетки к северу от него («\»), а
    /// западную — клетки к западу («/»); в северо-западном углу линии сходятся сами, без
    /// столбика. Южную и восточную стены камера видит спереди, и они закрывали бы подвал —
    /// их нет, а уйти с пола всё равно некуда: земля в пятидесяти единицах выше.
    ///
    /// Люк — в полу первого этажа, по нему ходят; двойной щелчок — вниз, к ступеням. Ступени
    /// стоят в северо-западном углу подвала, а не под люком: под люком игрок появляется сам,
    /// и щёлкнуть по ступеням у себя под ногами было нельзя — щелчок попадал в персонажа.
    /// </summary>
    private void BuildFixtures(List<Point3D> ground, HashSet<Point2D> columns, int z)
    {
        foreach (var c in columns)
        {
            if (!columns.Contains(new Point2D(c.X, c.Y - 1)))
            {
                AddWall(WallAlongX, c.X, c.Y - 1, z);
            }

            if (!columns.Contains(new Point2D(c.X - 1, c.Y)))
            {
                AddWall(WallAlongY, c.X - 1, c.Y, z);
            }
        }

        // Люк — на свободной клетке первого этажа подальше от стен.
        var spot = ground[ground.Count / 2];
        foreach (var t in ground)
        {
            if (AreaMap.CanFit(t, 16, false, false) && CountColumns(columns, t) == 9)
            {
                spot = t;
                break;
            }
        }

        // Ступени — в северо-западном углу, игрок появляется рядом с ними.
        var corner = default(Point2D);
        var best = int.MaxValue;
        foreach (var c in columns)
        {
            if (c.X + c.Y < best || c.X + c.Y == best && c.X < corner.X)
            {
                best = c.X + c.Y;
                corner = c;
            }
        }

        var arrival = new Point3D(corner.X, corner.Y, z);
        foreach (var (dx, dy) in new[] { (1, 0), (0, 1), (1, 1) })
        {
            if (columns.Contains(new Point2D(corner.X + dx, corner.Y + dy)))
            {
                arrival = new Point3D(corner.X + dx, corner.Y + dy, z);
                break;
            }
        }

        var down = new MahaonBasementHatch(this, arrival, "люк в подвал", HatchGraphic);
        down.MoveToWorld(spot, AreaMap);
        var up = new MahaonBasementHatch(this, spot, "ступени наверх", ExitStepsGraphic);
        up.MoveToWorld(new Point3D(corner.X, corner.Y, z), AreaMap);
        Basement.Add(down);
        Basement.Add(up);
    }

    private void AddWall(int graphic, int x, int y, int z)
    {
        var wall = new Static(graphic);
        wall.MoveToWorld(new Point3D(x, y, z), AreaMap);
        Basement.Add(wall);
    }

    /// <summary>
    /// Подвал, выкопанный до октября 2026 (стены по всему периметру с углами-столбиками,
    /// ступени вместо люка, выход под самим люком), перестраивается по-новому: пол остаётся,
    /// стены, люк и ступени ставятся заново. Узнаётся по люку — у старого он был ступенями.
    /// Зовётся при загрузке мира; у нового подвала ничего не делает. Возвращает, перестроен ли.
    /// </summary>
    public bool RefreshBasementFixtures()
    {
        if (!HasBasement || AreaMap == null)
        {
            return false;
        }

        var legacy = false;
        var floorZ = int.MinValue;

        foreach (var item in Basement)
        {
            if (item is MahaonBasementHatch { Deleted: false } hatch && hatch.Destination.Z < hatch.Z &&
                hatch.ItemID != HatchGraphic)
            {
                legacy = true;
            }

            if (item is Static { Deleted: false } floor && floor.ItemID == BasementFloorId)
            {
                floorZ = floor.Z;
            }
        }

        if (!legacy || floorZ == int.MinValue)
        {
            return false;
        }

        var keep = new List<Item>();
        foreach (var item in Basement)
        {
            if (item is Static { ItemID: BasementFloorId })
            {
                keep.Add(item);
            }
            else
            {
                item?.Delete();
            }
        }

        Basement.Clear();
        foreach (var item in keep)
        {
            Basement.Add(item);
        }

        var columns = new HashSet<Point2D>();
        foreach (var floor in keep)
        {
            columns.Add(new Point2D(floor.X, floor.Y));
        }

        var ground = GroundTiles();
        if (ground.Count > 0 && columns.Count > 0)
        {
            BuildFixtures(ground, columns, floorZ);
        }

        return true;
    }

    private bool HasItemAt(Point2D p, int z)
    {
        foreach (var item in AreaMap.GetItemsAt(p))
        {
            if ((item.Z - z).Abs() < 16)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountColumns(HashSet<Point2D> columns, Point3D t)
    {
        var count = 0;
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (columns.Contains(new Point2D(t.X + dx, t.Y + dy)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void DeleteBasement()
    {
        foreach (var item in Basement)
        {
            item?.Delete();
        }

        Basement.Clear();
    }

    /// <summary>Takes over a neighbour's cellar when the two houses become one.</summary>
    public void AdoptBasement(MahaonCityHouse from)
    {
        foreach (var item in from.Basement)
        {
            if (item is MahaonBasementHatch hatch)
            {
                hatch.House = this;
            }

            Basement.Add(item);
        }

        from.Basement.Clear();
        HasBasement |= from.HasBasement;
        from.HasBasement = false;
        this.MarkDirty();
    }

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

    /// <summary>Turns the house's furniture back into deeds for <paramref name="to"/>'s bank box —
    /// the house is changing hands, and the furniture is its old owner's.</summary>
    public void ReturnAddons(Mobile to)
    {
        foreach (var item in new List<Item>(Addons))
        {
            if (item is BaseAddon { Deleted: false } addon)
            {
                var deed = addon.Deed;
                addon.Delete();

                if (deed != null)
                {
                    if (to?.BankBox is { } bank)
                    {
                        bank.DropItem(deed);
                    }
                    else
                    {
                        deed.Delete();
                    }
                }
            }
        }

        Addons.Clear();
        this.MarkDirty();
    }

    /// <summary>Whether any tile of <paramref name="other"/> touches one of this house's tiles on
    /// the same floor — neighbours that can become one home.</summary>
    public bool Adjoins(MahaonCityHouse other)
    {
        if (other.AreaMap != AreaMap)
        {
            return false;
        }

        foreach (var a in Tiles)
        {
            foreach (var b in other.Tiles)
            {
                if ((a.X - b.X).Abs() <= 1 && (a.Y - b.Y).Abs() <= 1 && (a.Z - b.Z).Abs() <= ZTolerance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Adds floor tiles to the house; returns how many were new.</summary>
    public int AddTiles(IEnumerable<Point3D> tiles)
    {
        var added = 0;
        foreach (var t in tiles)
        {
            if (!Tiles.Contains(t))
            {
                Tiles.Add(t);
                added++;
            }
        }

        if (added > 0)
        {
            this.MarkDirty();
            ApplyFurnitureHiding();
            Systems.MahaonWorld.MahaonCityHouseSystem.Reindex(this);
        }

        return added;
    }

    /// <summary>Takes floor tiles off the house. Whatever was fixed on them is let go and the
    /// furniture standing there goes back to the owner as deeds.</summary>
    public int RemoveTiles(IEnumerable<Point3D> tiles)
    {
        var removed = new List<Point3D>();
        foreach (var t in tiles)
        {
            if (Tiles.Remove(t))
            {
                removed.Add(t);
            }
        }

        if (removed.Count == 0)
        {
            return 0;
        }

        foreach (var item in new List<Item>(Lockdowns))
        {
            if (item?.Deleted == false && !Contains(item.Location, item.Map))
            {
                ReleaseItem(item);
            }
        }

        foreach (var item in new List<Item>(Secures))
        {
            if (item?.Deleted == false && !Contains(item.Location, item.Map))
            {
                ReleaseItem(item);
            }
        }

        foreach (var item in new List<Item>(Addons))
        {
            if (item is BaseAddon { Deleted: false } addon && !Contains(addon.Location, addon.Map))
            {
                Addons.Remove(addon);
                var deed = addon.Deed;
                addon.Delete();
                if (deed != null && Owner?.BankBox is { } bank)
                {
                    bank.DropItem(deed);
                }
                else
                {
                    deed?.Delete();
                }
            }
        }

        this.MarkDirty();
        ApplyFurnitureHiding();
        Systems.MahaonWorld.MahaonCityHouseSystem.Reindex(this);
        return removed.Count;
    }

    /// <summary>Whether <paramref name="m"/> may open or use a fixed item of the house.</summary>
    public bool CanAccess(Mobile m, Item item) => !IsLockedDown(item) || IsFriend(m);

    public bool IsOwner(Mobile m) => m != null && (m == Owner || m.AccessLevel >= AccessLevel.GameMaster);

    public bool IsFriend(Mobile m) => m != null && (IsOwner(m) || Friends.Contains(m));

    public bool IsBanned(Mobile m) => m != null && Bans.Contains(m);

    public override void OnDelete()
    {
        ReleaseAll();
        ReturnAddons(Owner);
        DeleteBasement();
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

                // The floor itself stays: a flat walkable surface is what is stood on, not furniture.
                var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
                if (data.Surface && !data.Impassable && data.Height <= 1)
                {
                    continue;
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
