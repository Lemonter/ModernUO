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

    /// <summary>Высота пола подвала, или null, если подвала нет.</summary>
    public int? CellarZ
    {
        get
        {
            foreach (var item in Basement)
            {
                if (item is Static { Deleted: false, ItemID: BasementFloorId } floor)
                {
                    return floor.Z;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The ground floor: the tiles at the house's lowest level above the cellar. Клетки подвала
    /// сюда не входят: раньше «самым нижним этажом» после покупки подвала становился он сам, и
    /// перестройка ставила люк «в первый этаж» — то есть в подвал.
    /// </summary>
    public List<Point3D> GroundTiles()
    {
        var cellarZ = CellarZ;
        var minZ = int.MaxValue;
        foreach (var t in Tiles)
        {
            if (!IsCellar(t, cellarZ))
            {
                minZ = System.Math.Min(minZ, t.Z);
            }
        }

        var ground = new List<Point3D>();
        foreach (var t in Tiles)
        {
            if (!IsCellar(t, cellarZ) && t.Z - minZ <= ZTolerance)
            {
                ground.Add(t);
            }
        }

        return ground;
    }

    private static bool IsCellar(Point3D t, int? cellarZ) => cellarZ is { } z && (t.Z - z).Abs() <= ZTolerance;

    /// <summary>Столбцы, где у дома есть что-то кроме подвала: туда смотрит проход сквозь мебель
    /// (MahaonCityHouseSystem.IsInsideAnyHouse). Подвал, добранный до минимума, уходит под улицу
    /// и чужие стены — делать их проходимыми наверху нельзя.</summary>
    public HashSet<Point2D> AboveGroundColumns()
    {
        var cellarZ = CellarZ;
        var columns = new HashSet<Point2D>();
        foreach (var t in Tiles)
        {
            if (!IsCellar(t, cellarZ))
            {
                columns.Add(new Point2D(t.X, t.Y));
            }
        }

        return columns;
    }

    /// <summary>A cellar costs half what the ground floor above it does — but never less than the
    /// smallest cellar dug (<see cref="Systems.MahaonWorld.MahaonBasementLayout.MinSide"/> squared).</summary>
    public int BasementPrice
    {
        get
        {
            var min = Systems.MahaonWorld.MahaonBasementLayout.MinSide * Systems.MahaonWorld.MahaonBasementLayout.MinSide;
            return System.Math.Max(GroundTiles().Count, min) * Systems.MahaonWorld.MahaonCityHouseSystem.PricePerTile / 2;
        }
    }

    // Люк в полу первого этажа (корабельный люк — поверхность, по нему ходят) и каменные
    // ступени выхода в углу подвала.
    private const int HatchGraphic = 0x3EAE;
    private const int ExitStepsGraphic = 0x079E;

    /// <summary>
    /// Digs the cellar: a floor under the whole ground floor <see cref="BasementDepth"/> Z down —
    /// at least <see cref="Systems.MahaonWorld.MahaonBasementLayout.MinSide"/> tiles each way,
    /// however narrow the rooms above — walls along its north and west sides with torches on them,
    /// a hatch in the ground floor and steps back up in a corner. The cellar's tiles join the
    /// house, so its region, lockdowns and furniture work down there too.
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

        var columns = new HashSet<Point2D>();

        foreach (var t in ground)
        {
            columns.Add(new Point2D(t.X, t.Y));
        }

        var z = System.Math.Max(LowestZ, Systems.MahaonWorld.MahaonBasementLayout.Below(AreaMap, columns, ground[0].Z) - BasementDepth);

        Systems.MahaonWorld.MahaonBasementLayout.ExpandToMinimum(columns, p => CellarFree(p, z));

        var cellar = new List<Point3D>();
        foreach (var c in columns)
        {
            LayFloor(c, z);
            cellar.Add(new Point3D(c.X, c.Y, z));
        }

        // Пол — первым: по нему узнаётся высота подвала (CellarZ), а по ней — первый этаж.
        BuildFixtures(ground, columns, z);

        HasBasement = true;
        AddTiles(cellar);
        return true;
    }

    private void LayFloor(Point2D c, int z)
    {
        var floor = new Static(BasementFloorId);
        floor.MoveToWorld(new Point3D(c.X, c.Y, z), AreaMap);
        Basement.Add(floor);
    }

    // Под добранной клеткой не должно быть чужого подвала (свой и так в списке).
    private bool CellarFree(Point2D p, int z) =>
        !Systems.MahaonWorld.MahaonCityHouseSystem.IsClaimed(AreaMap, p.X, p.Y, z, 10, this);

    /// <summary>
    /// Стены, факелы, люк и ступени подвала поверх готового пола (MahaonBasementLayout).
    ///
    /// Люк — в полу первого этажа, по нему ходят; двойной щелчок — вниз, к ступеням. Ступени
    /// стоят в северо-западном углу подвала, а не под люком: под люком игрок появлялся сам,
    /// и щёлкнуть по ступеням у себя под ногами было нельзя — щелчок попадал в персонажа.
    /// </summary>
    private void BuildFixtures(List<Point3D> ground, HashSet<Point2D> columns, int z)
    {
        Systems.MahaonWorld.MahaonBasementLayout.BuildWalls(columns, z, AreaMap, Basement);

        // Люк — на свободной клетке первого этажа подальше от стен.
        var spot = ground[ground.Count / 2];
        foreach (var t in ground)
        {
            if (AreaMap.CanFit(t, 16, false, false) && Systems.MahaonWorld.MahaonBasementLayout.CountColumns(columns, t.X, t.Y) == 9)
            {
                spot = t;
                break;
            }
        }

        var corner = Systems.MahaonWorld.MahaonBasementLayout.Corner(columns);
        var arrival = Systems.MahaonWorld.MahaonBasementLayout.Arrival(columns, corner, z);

        var down = new MahaonBasementHatch(this, arrival, "люк в подвал", HatchGraphic);
        down.MoveToWorld(spot, AreaMap);
        var up = new MahaonBasementHatch(this, spot, "ступени наверх", ExitStepsGraphic);
        up.MoveToWorld(new Point3D(corner.X, corner.Y, z), AreaMap);
        Basement.Add(down);
        Basement.Add(up);

        Systems.MahaonWorld.MahaonBasementLayout.PlaceTorches(columns, z, AreaMap, corner, Basement);
        this.MarkDirty();
    }

    /// <summary>
    /// Приводит выкопанный раньше подвал к нынешнему виду, один раз при загрузке мира:
    /// <list type="bullet">
    /// <item>подвал до октября 2026 (стены по всему периметру, ступени вместо люка, выход под
    /// самим люком) — узнаётся по люку;</item>
    /// <item>подвал без факелов — до ноября 2026;</item>
    /// <item>подвал уже <see cref="Systems.MahaonWorld.MahaonBasementLayout.MinSide"/> клеток —
    /// добирается до минимума бесплатно.</item>
    /// </list>
    /// Пол остаётся, стены, факелы, люк и ступени ставятся заново. У нового подвала ничего не
    /// делает. Возвращает, перестроен ли.
    /// </summary>
    public bool RefreshBasementFixtures()
    {
        if (!HasBasement || AreaMap == null)
        {
            return false;
        }

        var legacy = false;
        var hasTorch = false;
        var floorZ = int.MinValue;
        var columns = new HashSet<Point2D>();

        foreach (var item in Basement)
        {
            if (item is MahaonBasementHatch { Deleted: false } hatch && hatch.Destination.Z < hatch.Z &&
                hatch.ItemID != HatchGraphic)
            {
                legacy = true;
            }

            if (item is { Deleted: false } && Systems.MahaonWorld.MahaonBasementLayout.IsTorch(item))
            {
                hasTorch = true;
            }

            if (item is Static { Deleted: false } floor && floor.ItemID == BasementFloorId)
            {
                floorZ = floor.Z;
                columns.Add(new Point2D(floor.X, floor.Y));
            }
        }

        var undersized = Systems.MahaonWorld.MahaonBasementLayout.IsUndersized(columns);

        // Подвал квартиры второго этажа, выкопанный до ноября 2026, лёг «на пятьдесят ниже
        // пола» — то есть на мостовую. Такой опускается под землю целиком.
        var ground = GroundTiles();
        var properZ = floorZ;
        if (floorZ != int.MinValue && ground.Count > 0)
        {
            properZ = System.Math.Max(
                LowestZ,
                Systems.MahaonWorld.MahaonBasementLayout.Below(AreaMap, columns, ground[0].Z) - BasementDepth
            );
        }

        var misplaced = floorZ - properZ >= 20;

        if (floorZ == int.MinValue || !legacy && hasTorch && !undersized && !misplaced)
        {
            return false;
        }

        if (misplaced)
        {
            LowerCellar(columns, floorZ, properZ);
            floorZ = properZ;
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

        if (undersized)
        {
            var cellar = new List<Point3D>();
            foreach (var c in Systems.MahaonWorld.MahaonBasementLayout.ExpandToMinimum(columns, p => CellarFree(p, floorZ)))
            {
                LayFloor(c, floorZ);
                cellar.Add(new Point3D(c.X, c.Y, floorZ));
            }

            AddTiles(cellar);
        }

        if (ground.Count > 0 && columns.Count > 0)
        {
            BuildFixtures(ground, columns, floorZ);
        }

        return true;
    }

    /// <summary>
    /// Опускает подвал с <paramref name="fromZ"/> на <paramref name="toZ"/>: плиты пола, клетки дома
    /// и своё у хозяина внизу — закреплённое, сундуки под замком, мебель из грамот. Чужое, что
    /// стоит на этой высоте (это же мостовая), не трогается. Стены, факелы и люки потом ставятся
    /// заново.
    /// </summary>
    private void LowerCellar(HashSet<Point2D> columns, int fromZ, int toZ)
    {
        var shift = toZ - fromZ;

        bool InOldCellar(Item item) =>
            item?.Deleted == false && item.Parent == null && item.Map == AreaMap &&
            columns.Contains(new Point2D(item.X, item.Y)) && item.Z >= fromZ - ZTolerance && item.Z < fromZ + 20;

        foreach (var item in Basement)
        {
            if (item is Static { ItemID: BasementFloorId } floor && !floor.Deleted)
            {
                floor.Z = toZ;
            }
        }

        var own = new HashSet<Item>();
        own.UnionWith(Lockdowns);
        own.UnionWith(Secures);
        own.UnionWith(Addons);

        foreach (var item in own)
        {
            if (InOldCellar(item))
            {
                item.Location = new Point3D(item.X, item.Y, item.Z + shift);
            }
        }

        var old = new List<Point3D>();
        var moved = new List<Point3D>();
        foreach (var t in Tiles)
        {
            if ((t.Z - fromZ).Abs() <= ZTolerance && columns.Contains(new Point2D(t.X, t.Y)))
            {
                old.Add(t);
                moved.Add(new Point3D(t.X, t.Y, toZ));
            }
        }

        // Напрямую, а не через RemoveTiles: тот отпустил бы закреплённое внизу и вернул мебель в
        // банк — а она едет вниз вместе с подвалом.
        foreach (var t in old)
        {
            Tiles.Remove(t);
        }

        AddTiles(moved);
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

    // ---- Очистка при покупке ------------------------------------------------------------------

    // Вещи этажа — от самого пола и до следующего этажа (он на двадцать выше): посуда стоит на
    // столе, а не на полу, и прежний допуск в три единицы её не видел.
    private const int FurnitureHeadroom = 18;

    // Что от здания, а не обстановка: стены, крыша, окна, двери, ступени и мосты.
    private const TileFlag StructureFlags = TileFlag.Wall | TileFlag.Roof | TileFlag.Window | TileFlag.Door |
                                            TileFlag.Bridge | TileFlag.StairBack | TileFlag.StairRight;

    /// <summary>
    /// Выносит из купленного дома всю обстановку города: мебель, посуду, светильники, сундуки —
    /// и нарисованную в карте статику (по-настоящему, через MahaonMapEdits, у сервера и у всех
    /// клиентов), и предметы декораций. Здание остаётся: стены, пол, ступени, окна, двери,
    /// ковры-полы. Своё у дома не трогается — закреплённое, сундуки под замком, мебель из
    /// грамот, подвал. Отвечает, сколько вынесено.
    /// </summary>
    public int ClearFurniture()
    {
        if (AreaMap == null || AreaMap == Map.Internal)
        {
            return 0;
        }

        var cellarZ = CellarZ;
        var removed = 0;

        var own = new HashSet<Item>();
        own.UnionWith(Lockdowns);
        own.UnionWith(Secures);
        own.UnionWith(Addons);
        own.UnionWith(Basement);

        Systems.MahaonMapEdits.MahaonMapEdits.BeginBatch();
        try
        {
            foreach (var t in Tiles)
            {
                if (IsCellar(t, cellarZ))
                {
                    continue;
                }

                foreach (var st in Systems.MahaonMapEdits.MahaonMapEdits.StaticsAt(AreaMap, t.X, t.Y))
                {
                    if (st.Z < t.Z - ZTolerance || st.Z >= t.Z + FurnitureHeadroom || IsStructure(st.Graphic))
                    {
                        continue;
                    }

                    if (Systems.MahaonMapEdits.MahaonMapEdits.RemoveStatic(AreaMap, t.X, t.Y, st.Graphic, st.Z))
                    {
                        removed++;
                    }
                }
            }
        }
        finally
        {
            Systems.MahaonMapEdits.MahaonMapEdits.EndBatch();
        }

        // Сперва собрать, потом удалять: перебор секторов карты ленивый.
        var doomed = new HashSet<Item>();
        foreach (var t in Tiles)
        {
            if (IsCellar(t, cellarZ))
            {
                continue;
            }

            foreach (var item in AreaMap.GetItemsAt(t.X, t.Y))
            {
                if (item.Z >= t.Z - ZTolerance && item.Z < t.Z + FurnitureHeadroom && IsDecoration(item, own))
                {
                    doomed.Add(item);
                }
            }
        }

        foreach (var item in doomed)
        {
            if (!item.Deleted)
            {
                item.Delete();
                removed++;
            }
        }

        if (removed > 0)
        {
            // Подмены «не рисовать» указывали на уже вынесенное — пересобрать по тому, что осталось.
            ApplyFurnitureHiding();
        }

        return removed;
    }

    private static bool IsStructure(int graphic)
    {
        var data = TileData.ItemTable[graphic & TileData.MaxItemValue];

        if ((data.Flags & StructureFlags) != 0)
        {
            return true;
        }

        // Пол, лестница, настил — по ним ходят.
        if (data.Surface && !data.Impassable)
        {
            return true;
        }

        // Ковёр, выложенный вместо пола: плоский и рисуется под всем остальным.
        return (data.Flags & TileFlag.Background) != 0 && data.Height <= 1;
    }

    private static bool IsDecoration(Item item, HashSet<Item> own)
    {
        if (item.Movable || item.IsLockedDown || item.IsSecure || own.Contains(item))
        {
            return false;
        }

        if (item is AddonComponent { Addon: { } addon } && own.Contains(addon))
        {
            return false;
        }

        if (item is BaseDoor or ISpawner or MahaonCityHouse or MahaonCityHouseSign or MahaonBasementHatch
            or BaseMulti)
        {
            return false;
        }

        return item is Static or LocalizedStatic or BaseLight or Container or BaseAddon or AddonComponent ||
               FurnitureAttribute.Check(item);
    }
}
