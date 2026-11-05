using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Blackthorn;

public enum InvasionType
{
    Dragons,
    Undead,
    Elemental,
    Daemon,
    Orc,
    Wildlings,
    Frost,
    Arachnid,
    Fey,
    Nature
}

/// <summary>
///     ServUO's <c>Server.Engines.CityLoyalty.City</c> — the nine loyalty cities, in the same order.
///     The City Loyalty system has no port here, and declaring a second <c>City</c> would collide with it
///     whenever it arrives, so the enum is renamed. Values are unchanged, and the invasion only ever
///     uses them as indices into <see cref="InvasionController.Defs" />.
/// </summary>
public enum InvasionCity
{
    Moonglow,
    Britain,
    Jhelom,
    Yew,
    Minoc,
    Trinsic,
    SkaraBrae,
    NewMagincia,
    Vesper
}

/// <summary>
///     Ported from ServUO's Blackthorn invasion spawner (InvasionSpawner/InvasionController.cs) — the
///     item that runs Minax's invasion on one facet: it picks a city and a creature family, raises the
///     beacon there, spawns waves of that family plus <see cref="Invader" />s and a blessed
///     <see cref="InvaderCaptain" /> per group, and ends the event (handing out the crest artifacts) when
///     the beacon falls.
///
///     What this port left out, all deliberate:
///       - <c>PointsSystem.Blackthorn</c> / <c>PointsSystem.FellowshipData</c> — this codebase has no
///         points framework. The per-player tally lives in this item as two parallel lists (the same
///         shape as the Covetous void pool's), read/written through <see cref="GetPoints" />,
///         <see cref="AwardPoints" /> and <see cref="SpendPoints" />. Nothing here awards points: in
///         ServUO the Blackthorn currency came from the Agent of the Crown, not from the invasion.
///         The tally is per-controller, i.e. per facet.
///       - <c>BlackthornRewards</c> / its gump — owned by the reward side of the port; this item only
///         exposes the points API.
///       - <c>RunicReforging.GenerateRandomItem(..., ReforgedPrefix.None, ReforgedSuffix.Minax)</c> in
///         <see cref="CreateItem" /> — reforging does not exist here, so the artifact handed out is the
///         plain loot roll with no Minax crest / reforged properties.
///       - <c>Server.Engines.XmlSpawner2.XmlAttach</c> ("Notoriety", "red") applied to every spawned
///         creature — the XmlSpawner attachment API has no counterpart. The invaders are red through
///         <see cref="Invader.AlwaysMurderer" />; the stock creatures in the spawn table are not.
///       - <c>Map.GetRandomSpawnPoint(Rectangle2D)</c> — replaced with a random point in the rectangle
///         plus the surface height (see <see cref="GetRandomSpawnPoint" />).
///       - The <c>Timer.DelayCall(TimeSpan.FromSeconds(30), CleanupSpawn)</c> that ServUO armed from
///         inside <c>Serialize</c>: a save-time side effect, not carried over.
///
///     The spawn ledger is three flat lists instead of ServUO's
///     <c>Dictionary&lt;BaseCreature, List&lt;BaseCreature&gt;&gt;</c>: the serializer handles
///     <c>List&lt;BaseCreature&gt;</c> but not nested collections, so each minion carries its
///     captain in a parallel list. All three are persisted, so an in-flight wave survives a restart.
///
///     Deaths reach <see cref="OnDeath" /> two ways: the Blackthorn dungeon region's <c>OnDeath</c> hook
///     (ServUO's route, kept in Regions/BlackthornDungeonRegion.cs), and a five-second sweep of this
///     item's own ledger, added because the invaded cities are not inside that region's area.
/// </summary>
[SerializationGenerator(0, false)]
public partial class InvasionController : Item
{
    public static bool Enabled = true;
    public static int WaveCountMin = 8;
    public static int WaveCountMax = 10;
    public static int MaxWaves = 2;

    /// <summary>The NewMagincia dock rectangle whose ledge the map fit query rejects (ServUO exempted it).</summary>
    private const int ExemptRectX = 6444;
    private const int ExemptRectY = 2446;

    private static readonly Point3D ControllerLocation = new(6359, 2570, 0);

    private static readonly InvasionType[] InvasionTypes = Enum.GetValues<InvasionType>();
    private static readonly InvasionCity[] InvasionCities = Enum.GetValues<InvasionCity>();

    private static readonly Type[][] SpawnTable =
    {
        new[] { typeof(Dragon), typeof(Drake), typeof(GiantSerpent), typeof(Reptalon), typeof(Hydra) },
        new[] { typeof(Lich), typeof(Wraith), typeof(Mummy), typeof(Zombie), typeof(SkeletalKnight), typeof(BoneKnight) },
        new[]
        {
            typeof(MudElemental), typeof(MoltenEarthElemental), typeof(DiseasedBloodElemental), typeof(GreaterAirElemental),
            typeof(GreaterBloodElemental), typeof(GreaterEarthElemental), typeof(GreaterWaterElemental),
            typeof(ShameGreaterPoisonElemental), typeof(StoneElemental)
        },
        new[] { typeof(Daemon), typeof(Succubus), typeof(Imp), typeof(ChaosDaemon), typeof(BoneDemon) },
        new[] { typeof(Orc), typeof(OrcBomber), typeof(OrcishMage), typeof(OrcishLord) },
        new[] { typeof(SwampTentacle), typeof(PlagueBeast), typeof(Bogling), typeof(FeralTreefellow) },
        new[] { typeof(IceElemental), typeof(SnowElemental), typeof(IceFiend), typeof(FrostTroll), typeof(IceSerpent) },
        new[] { typeof(DreadSpider), typeof(GiantBlackWidow), typeof(Scorpion), typeof(TerathanWarrior), typeof(WolfSpider) },
        new[] { typeof(Satyr), typeof(Centaur), typeof(CuSidhe), typeof(Wisp), typeof(MLDryad) },
        new[] { typeof(DireWolf), typeof(GiantRat), typeof(Troglodyte), typeof(RagingGrizzlyBear), typeof(GreaterMongbat) }
    };

    /// <summary>City → invasion layout. Built once at type load so it is already populated when a saved
    /// controller deserializes (ServUO filled it in <c>Initialize</c>, which runs after world load).</summary>
    public static readonly Dictionary<InvasionCity, InvasionDefinition> Defs = CreateDefs();

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private InvasionBeacon _beacon;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private InvasionCity _currentInvasion;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private InvasionType _invasionType;

    [SerializableField(3, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _currentWave;

    [SerializableField(4)]
    private List<BaseCreature> _captains;

    [SerializableField(5)]
    private List<BaseCreature> _minions;

    /// <summary>Parallel to <see cref="_minions" />: the Captain each minion was spawned for.</summary>
    [SerializableField(6)]
    private List<BaseCreature> _minionOwners;

    [SerializableField(7)]
    private List<Mobile> _pointHolders;

    [SerializableField(8)]
    private List<int> _pointValues;

    private List<Rectangle2D> _spawnZones;
    private TimerExecutionToken _startToken;
    private TimerExecutionToken _restartToken;
    private TimerExecutionToken _sweepToken;
    private bool _sweepRunning;

    public InvasionController(Map map) : base(3796)
    {
        Movable = false;
        Visible = false;

        _captains = new List<BaseCreature>();
        _minions = new List<BaseCreature>();
        _minionOwners = new List<BaseCreature>();
        _pointHolders = new List<Mobile>();
        _pointValues = new List<int>();
        _spawnZones = new List<Rectangle2D>();

        if (map == Map.Trammel)
        {
            TramInstance = this;
        }
        else if (map == Map.Felucca)
        {
            FelInstance = this;
        }

        StartSweep();

        if (Enabled)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(10), BeginInvasion, out _startToken);
        }
    }

    public static InvasionController TramInstance { get; set; }

    public static InvasionController FelInstance { get; set; }

    /// <summary>True while an invasion is running — the flag the Blackthorn dungeon region gates its
    /// noxious-cloud sweep on.</summary>
    public static bool InProgress => BlackthornDungeonRegion.InProgress;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ForceRespawn
    {
        get => false;
        set
        {
            if (!value)
            {
                return;
            }

            RemoveSpawn();
            OnEndInvasion();
            Cleanup();
            BeginInvasion();
        }
    }

    /// <summary>The zones this invasion draws its spawn rectangles from. Rebuilt from
    /// <see cref="Defs" /> after a load rather than persisted (it is a shuffled copy of static data).</summary>
    public List<Rectangle2D> SpawnZones => _spawnZones;

    [CommandProperty(AccessLevel.GameMaster)]
    public int SpawnCount
    {
        get
        {
            var count = 0;

            for (var i = 0; i < _captains.Count; i++)
            {
                if (_captains[i] is { Alive: true })
                {
                    count++;
                }
            }

            for (var i = 0; i < _minions.Count; i++)
            {
                if (_minions[i] is { Alive: true })
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>ServUO's rule: the beacon only accepts damage once no Captain of the current wave is left.</summary>
    public bool BeaconVulnerable => Beacon != null && _captains.Count == 0;

    public void BeginInvasion()
    {
        var map = Map;

        if (!Enabled || Deleted || map == null || map == Map.Internal)
        {
            return;
        }

        RemoveSpawn();

        CurrentWave = 1;

        InvasionType newType;
        do
        {
            newType = InvasionTypes[Utility.Random(InvasionTypes.Length)];
        }
        while (newType == InvasionType);

        InvasionCity newCity;
        do
        {
            newCity = InvasionCities[Utility.Random(InvasionCities.Length)];
        }
        while (newCity == CurrentInvasion);

        CurrentInvasion = newCity;
        InvasionType = newType;

        _spawnZones = new List<Rectangle2D>(Defs[CurrentInvasion].SpawnRecs);

        if (Beacon != null && !Beacon.Deleted)
        {
            Beacon.Delete();
        }

        Beacon = new InvasionBeacon(this);
        Beacon.MoveToWorld(Defs[CurrentInvasion].BeaconLoc, map);

        // Shuffle the zones so the first rectangle of the table is not always the first one hit.
        for (var i = 0; i < 8; i++)
        {
            if (_spawnZones.Count == 0)
            {
                break;
            }

            var rec = _spawnZones[Utility.Random(_spawnZones.Count)];
            _spawnZones.Remove(rec);
            _spawnZones.Insert(0, rec);
        }

        BlackthornDungeonRegion.InProgress = true;

        SpawnWave();
    }

    public void SpawnWave()
    {
        var map = Map;

        if (map == null || map == Map.Internal || _spawnZones == null || _spawnZones.Count == 0)
        {
            return;
        }

        var zones = new List<Rectangle2D>(_spawnZones);
        var types = SpawnTable[(int)_invasionType];

        for (var group = 0; group < 2 && zones.Count > 0; group++)
        {
            var spawnRec = zones[Utility.Random(zones.Count)];
            zones.Remove(spawnRec);

            var count = Utility.RandomMinMax(WaveCountMin, WaveCountMax);
            var minions = new List<BaseCreature>(count + 3);

            for (var i = 0; i < count; i++)
            {
                if (Activator.CreateInstance(types[Utility.Random(types.Length)]) is not BaseCreature creature)
                {
                    continue;
                }

                if (SpawnMobile(creature, spawnRec))
                {
                    minions.Add(creature);
                }
                else
                {
                    creature.Delete();
                }
            }

            for (var i = 0; i < 3; i++)
            {
                var invader = new Invader(InvasionType);

                if (SpawnMobile(invader, spawnRec))
                {
                    minions.Add(invader);
                }
                else
                {
                    invader.Delete();
                }
            }

            var captain = new InvaderCaptain(InvasionType) { Blessed = true };
            var beaconLoc = Defs[CurrentInvasion].BeaconLoc;
            var beaconRec = new Rectangle2D(beaconLoc.X - 10, beaconLoc.Y - 10, 20, 20);

            if (SpawnMobile(captain, spawnRec) || SpawnMobile(captain, beaconRec))
            {
                AddGroup(captain, minions);
            }
            else
            {
                captain.Delete();
            }
        }
    }

    private void AddGroup(BaseCreature captain, List<BaseCreature> minions)
    {
        _captains.Add(captain);

        for (var i = 0; i < minions.Count; i++)
        {
            _minions.Add(minions[i]);
            _minionOwners.Add(captain);
        }
    }

    private bool SpawnMobile(BaseCreature creature, Rectangle2D spawnRec)
    {
        var map = Map;

        if (map == null || map == Map.Internal || creature == null)
        {
            return false;
        }

        var exempt = spawnRec.X == ExemptRectX && spawnRec.Y == ExemptRectY;

        for (var i = 0; i < 25; i++)
        {
            var p = GetRandomSpawnPoint(spawnRec, map);

            if (exempt)
            {
                p.Z = -2;
            }

            if (exempt || map.CanFit(p.X, p.Y, p.Z, 16, false, false, true))
            {
                creature.MoveToWorld(p, map);
                creature.Home = Defs[CurrentInvasion].BeaconLoc;
                creature.HomeMap = map;
                creature.SeeksHome = true;
                creature.RangeHome = Utility.RandomMinMax(5, 10);
                creature.CanSwim = false;
                creature.Tamable = false;

                return true;
            }
        }

        return false;
    }

    private static Point3D GetRandomSpawnPoint(Rectangle2D rec, Map map)
    {
        var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width - 1);
        var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height - 1);

        return new Point3D(x, y, map.GetAverageZ(x, y));
    }

    /// <summary>Called for every tracked creature that dies: by the Blackthorn dungeon region for
    /// deaths inside it, and by this item's own sweep for anything the region does not see. Keeps the
    /// wave ledger honest: a fallen Captain completes the wave once the last one is gone, a fallen
    /// minion un-blesses its Captain when its group is wiped. Idempotent, so both callers can overlap.</summary>
    public void OnDeath(BaseCreature creature)
    {
        if (creature == null || creature.Controlled || creature.Summoned)
        {
            return;
        }

        if (RemoveCaptain(creature))
        {
            if (_captains.Count == 0)
            {
                CompleteWave();
            }

            return;
        }

        RemoveMinion(creature);
    }

    private bool RemoveCaptain(BaseCreature captain)
    {
        var index = _captains.IndexOf(captain);

        if (index < 0)
        {
            return false;
        }

        // A fallen Captain's minions stay in the world but are no longer tracked, exactly as ServUO
        // dropped the captain's dictionary entry (and the group with it).
        for (var i = _minions.Count - 1; i >= 0; i--)
        {
            if (_minionOwners[i] == captain)
            {
                _minions.RemoveAt(i);
                _minionOwners.RemoveAt(i);
            }
        }

        _captains.RemoveAt(index);

        return true;
    }

    private void RemoveMinion(BaseCreature minion)
    {
        var index = _minions.IndexOf(minion);

        if (index < 0)
        {
            return;
        }

        var captain = _minionOwners[index];

        _minions.RemoveAt(index);
        _minionOwners.RemoveAt(index);

        if (captain == null || !captain.Alive)
        {
            return;
        }

        if (CountAliveMinions(captain) == 0)
        {
            captain.Blessed = false;
            captain.Delta(MobileDelta.Noto);
        }
    }

    private int CountAliveMinions(BaseCreature captain)
    {
        var count = 0;

        for (var i = 0; i < _minions.Count; i++)
        {
            if (_minionOwners[i] == captain && _minions[i] is { Alive: true })
            {
                count++;
            }
        }

        return count;
    }

    private void CompleteWave()
    {
        DoMessage();

        if (CurrentWave < MaxWaves)
        {
            CurrentWave++;
        }
    }

    /// <summary>ServUO learned about deaths from its Blackthorn dungeon region's <c>OnDeath</c> hook,
    /// which this port keeps (see <see cref="BlackthornDungeonRegion" />). That region only exists where
    /// its data has been generated, and the invasion is raised in the invaded cities, so this sweep is
    /// the safety net that keeps a wave moving even when no region sees the kill. It only walks this
    /// controller's own handful of creatures.</summary>
    private void StartSweep()
    {
        if (_sweepRunning)
        {
            return;
        }

        _sweepRunning = true;
        Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), ProcessDeadSpawn, out _sweepToken);
    }

    private void StopSweep()
    {
        if (!_sweepRunning)
        {
            return;
        }

        _sweepRunning = false;
        _sweepToken.Cancel();
    }

    private void ProcessDeadSpawn()
    {
        // Each pass drops at least one ledger entry, so this terminates; the bound is belt and braces.
        for (var guard = 0; guard < 128; guard++)
        {
            var captainIndex = IndexOfDeadCaptain();

            if (captainIndex >= 0)
            {
                var captain = _captains[captainIndex];

                if (captain == null || captain.Deleted)
                {
                    DropCaptain(captainIndex);
                }
                else
                {
                    OnDeath(captain);
                }

                continue;
            }

            var minionIndex = IndexOfDeadMinion();

            if (minionIndex < 0)
            {
                return;
            }

            var minion = _minions[minionIndex];

            if (minion == null || minion.Deleted)
            {
                DropMinion(minionIndex);
            }
            else
            {
                OnDeath(minion);
            }
        }
    }

    private int IndexOfDeadCaptain()
    {
        for (var i = 0; i < _captains.Count; i++)
        {
            if (_captains[i] is not { Alive: true })
            {
                return i;
            }
        }

        return -1;
    }

    private int IndexOfDeadMinion()
    {
        for (var i = 0; i < _minions.Count; i++)
        {
            if (_minions[i] is not { Alive: true })
            {
                return i;
            }
        }

        return -1;
    }

    private void DropCaptain(int index)
    {
        var captain = _captains[index];

        for (var i = _minions.Count - 1; i >= 0; i--)
        {
            if (_minionOwners[i] == captain)
            {
                _minions.RemoveAt(i);
                _minionOwners.RemoveAt(i);
            }
        }

        _captains.RemoveAt(index);
    }

    private void DropMinion(int index)
    {
        _minions.RemoveAt(index);
        _minionOwners.RemoveAt(index);
    }

    private void DoMessage()
    {
        var map = Map;

        if (map == null || Beacon == null)
        {
            return;
        }

        foreach (var m in map.GetMobilesInRange(Beacon.Location, 20))
        {
            if (m?.NetState != null)
            {
                // *A sound roars in the distance...Minax's Beacon is vulnerable to attack!!*
                m.PrivateOverheadMessage(MessageType.Regular, 1154, 1154550, m.NetState);
            }
        }
    }

    public void OnBeaconDestroyed()
    {
        OnEndInvasion();

        _restartToken.Cancel();
        Timer.StartTimer(TimeSpan.FromMinutes(2), RestartInvasion, out _restartToken);
    }

    private void RestartInvasion()
    {
        if (Deleted)
        {
            return;
        }

        Cleanup();
        BeginInvasion();
    }

    public void OnEndInvasion()
    {
        BlackthornDungeonRegion.InProgress = false;

        if (Beacon == null)
        {
            return;
        }

        var rights = Beacon.GetLootingRights();

        foreach (var damager in rights)
        {
            if (damager == null || !damager.InRange(Beacon.Location, 12))
            {
                continue;
            }

            if (0.15 < Utility.RandomDouble())
            {
                continue;
            }

            var item = CreateItem(damager);

            if (item == null)
            {
                continue;
            }

            damager.PlaySound(0x5B4);
            damager.SendLocalizedMessage(1154554); // You recover an artifact bearing the crest of Minax from the rubble.

            if (!damager.PlaceInBackpack(item))
            {
                if (damager.BankBox != null && damager.BankBox.TryDropItem(damager, item, false))
                {
                    damager.SendLocalizedMessage(1079730); // The item has been placed into your bank box.
                }
                else
                {
                    damager.SendLocalizedMessage(1072523); // You find an artifact, but your backpack and bank are too full to hold it.
                    item.MoveToWorld(damager.Location, damager.Map);
                }
            }
        }
    }

    /// <summary>ServUO reforged the reward through <c>RunicReforging</c> with a Minax suffix; reforging is
    /// not part of this codebase, so the loot roll is the whole artifact. It is stamped with the marker name
    /// the Agent of the Crown recognises as turn-in material (there is no crest property to check).</summary>
    public static Item CreateItem(Mobile damager)
    {
        var item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();

        if (item != null)
        {
            item.Name = AgentOfTheCrown.MinaxArtifactName;
            item.Hue = 0x21; // Minax red
        }

        return item;
    }

    public void Cleanup()
    {
        if (Beacon != null && !Beacon.Deleted)
        {
            Beacon.Delete();
        }

        Beacon = null;
    }

    public void RemoveSpawn()
    {
        if (_captains == null)
        {
            return;
        }

        // Copy first: Kill() fires the region hook, which mutates these lists.
        var captains = new List<BaseCreature>(_captains);
        var minions = new List<BaseCreature>(_minions);

        _captains.Clear();
        _minions.Clear();
        _minionOwners.Clear();

        for (var i = 0; i < minions.Count; i++)
        {
            if (minions[i] is { Alive: true } minion)
            {
                minion.Kill();
            }
        }

        for (var i = 0; i < captains.Count; i++)
        {
            if (captains[i] is { Alive: true } captain)
            {
                captain.Blessed = false;
                captain.Kill();
            }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.GameMaster)
        {
            from.SendGump(new PropertiesGump(from, this));
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _startToken.Cancel();
        _restartToken.Cancel();
        StopSweep();

        RemoveSpawn();
        Cleanup();

        BlackthornDungeonRegion.InProgress = false;

        if (TramInstance == this)
        {
            TramInstance = null;
        }

        if (FelInstance == this)
        {
            FelInstance = null;
        }

        _pointHolders?.Clear();
        _pointValues?.Clear();
    }

    /// <summary>Points banked by a player on this facet, spent by the Blackthorn reward gump.</summary>
    public int GetPoints(Mobile m)
    {
        if (m == null)
        {
            return 0;
        }

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
    }

    public bool SpendPoints(Mobile m, int amount)
    {
        if (m == null || amount <= 0)
        {
            return false;
        }

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

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _captains ??= new List<BaseCreature>();
        _minions ??= new List<BaseCreature>();
        _minionOwners ??= new List<BaseCreature>();
        _pointHolders ??= new List<Mobile>();
        _pointValues ??= new List<int>();

        if (Map == Map.Trammel)
        {
            TramInstance = this;
        }
        else if (Map == Map.Felucca)
        {
            FelInstance = this;
        }

        if (Beacon != null)
        {
            Beacon.Controller = this;
        }

        if (_spawnZones == null || _spawnZones.Count == 0)
        {
            _spawnZones = Defs.TryGetValue(CurrentInvasion, out var def)
                ? new List<Rectangle2D>(def.SpawnRecs)
                : new List<Rectangle2D>();
        }

        TidySpawn();
        StartSweep();

        if (Beacon == null || Beacon.Destroyed)
        {
            // The beacon fell before the save (or never existed): ServUO waited two minutes and then
            // raised a fresh invasion rather than resuming a wave nobody can win.
            _restartToken.Cancel();
            Timer.StartTimer(TimeSpan.FromMinutes(2), RestartInvasion, out _restartToken);
        }
        else
        {
            BlackthornDungeonRegion.InProgress = true;
        }
    }

    /// <summary>Re-aligns the spawn ledger after a load: deletes remove entries from one list at a time,
    /// so a minion and its Captain are dropped as a pair here.</summary>
    private void TidySpawn()
    {
        for (var i = _minions.Count - 1; i >= 0; i--)
        {
            if (i >= _minionOwners.Count)
            {
                _minions.RemoveAt(i);
                continue;
            }

            if (_minions[i] is not { Deleted: false } || _minionOwners[i] is not { Deleted: false })
            {
                _minions.RemoveAt(i);
                _minionOwners.RemoveAt(i);
            }
        }

        for (var i = _captains.Count - 1; i >= 0; i--)
        {
            if (_captains[i] is not { Deleted: false })
            {
                _captains.RemoveAt(i);
            }
        }
    }

    public static void Initialize()
    {
        // The statics are also set from AfterDeserialization (deferred until every entity is loaded),
        // so the world is checked too: a save whose controllers are still loading must not get a second
        // pair raised on top of them.
        if (TramInstance == null && !HasInstance(Map.Trammel))
        {
            TramInstance = new InvasionController(Map.Trammel);
            TramInstance.MoveToWorld(ControllerLocation, Map.Trammel);
        }

        if (FelInstance == null && !HasInstance(Map.Felucca))
        {
            FelInstance = new InvasionController(Map.Felucca);
            FelInstance.MoveToWorld(ControllerLocation, Map.Felucca);
        }
    }

    private static bool HasInstance(Map map)
    {
        if (map == null)
        {
            return false;
        }

        foreach (var item in map.GetItemsInRange(ControllerLocation, 0))
        {
            if (item is InvasionController)
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<InvasionCity, InvasionDefinition> CreateDefs() => new()
    {
        [InvasionCity.Moonglow] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6314, 2571, 10, 5),
                new Rectangle2D(6288, 2535, 8, 15),
                new Rectangle2D(6322, 2527, 8, 8),
                new Rectangle2D(6302, 2524, 10, 5)
            },
            new Point3D(6317, 2555, 0)
        ),
        [InvasionCity.Britain] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6296, 2464, 7, 7),
                new Rectangle2D(6332, 2473, 8, 10),
                new Rectangle2D(6320, 2508, 3, 8),
                new Rectangle2D(6287, 2494, 8, 8)
            },
            new Point3D(6316, 2477, 11)
        ),
        [InvasionCity.Jhelom] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6450, 2465, 10, 8),
                new Rectangle2D(6418, 2497, 15, 5),
                new Rectangle2D(6417, 2469, 5, 10),
                new Rectangle2D(6432, 2507, 10, 5)
            },
            new Point3D(6448, 2492, 5)
        ),
        [InvasionCity.Yew] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6314, 2397, 12, 5),
                new Rectangle2D(6317, 2440, 10, 10),
                new Rectangle2D(6286, 2432, 8, 8),
                new Rectangle2D(6289, 2405, 5, 5)
            },
            new Point3D(6305, 2423, 0)
        ),
        [InvasionCity.Minoc] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6309, 2339, 10, 5),
                new Rectangle2D(6290, 2367, 5, 10),
                new Rectangle2D(6304, 2378, 10, 5),
                new Rectangle2D(6323, 2344, 5, 10)
            },
            new Point3D(6307, 2362, 15)
        ),
        [InvasionCity.Trinsic] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6356, 2371, 10, 10),
                new Rectangle2D(6354, 2344, 5, 10),
                new Rectangle2D(6366, 2344, 5, 7),
                new Rectangle2D(6386, 2355, 8, 8)
            },
            new Point3D(6402, 2368, 25)
        ),
        [InvasionCity.SkaraBrae] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6434, 2330, 10, 5),
                new Rectangle2D(6456, 2342, 5, 10),
                new Rectangle2D(6458, 2368, 15, 6),
                new Rectangle2D(6440, 2384, 10, 3),
                new Rectangle2D(6412, 2360, 12, 12)
            },
            new Point3D(6442, 2351, 0)
        ),
        [InvasionCity.NewMagincia] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6426, 2397, 10, 5),
                new Rectangle2D(6444, 2446, 10, 5),
                new Rectangle2D(6436, 2395, 5, 8),
                new Rectangle2D(6419, 2446, 10, 5)
            },
            new Point3D(6440, 2419, 26)
        ),
        [InvasionCity.Vesper] = new InvasionDefinition(
            new[]
            {
                new Rectangle2D(6428, 2534, 10, 5),
                new Rectangle2D(6458, 2534, 5, 10),
                new Rectangle2D(6460, 2551, 5, 10),
                new Rectangle2D(6433, 2561, 6, 6)
            },
            new Point3D(6444, 2553, 0)
        )
    };
}
