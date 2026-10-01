using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Despise;

/// <summary>
///     Ported from ServUO's Despise Revamped dungeon (Scripts/Services/Dungeons/
///     DespiseRevamped/DespiseController.cs) — the singleton driving the "Call to Arms"
///     boss-encounter cycle (accumulate WispOrb army power on each side, summon the
///     stronger side's overlord, players escort their possessed creature through the fight).
///
///     Left out of this port, all deliberately:
///       - The ML Quest tie-in (Quests.WhisperingWithWispsQuest/QuestHelper/
///         MondainQuestGump/TownCryerSystem) — narrative flavor layered on top of the
///         dungeon, not the mechanic itself. Orb-granting via the Ankh still works, just
///         without a quest chain gating/rewarding it.
///       - ServUO's generic Engines.Points.PointsSystem framework — nothing else in this
///         codebase uses it, so PutridHeart's point payout goes through a small standalone
///         tracker instead (see DespiseCrystals.cs) rather than porting the whole framework
///         for one reward currency.
///       - ColUtility.Free(...) calls — that's a ServUO object-pool return call with no
///         equivalent here; dropped as a no-op (GC handles it).
///       - The original's multi-version Serialize/Deserialize migration branches (this
///         class went through 5 serialization versions in ServUO patching around old
///         save-compatibility) — irrelevant for a fresh port with no existing save data, so
///         this only writes/reads the CURRENT shape.
///
///     IPooledEnumerable-based enumeration is gone here like everywhere else in this port —
///     converted to plain foreach over Map.GetMobilesInRange/GetItemsInRange.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DespiseController : Item
{
    // Was EventSink.Login (a LoginEventArgs-wrapped event) — this codebase's closest
    // equivalent is EventSink.Connected, which fires with the Mobile directly, no wrapper
    // type. There's also no EventSink.OnEnterRegion here at all (no global "entered some
    // region" event) — that hook's logic (delete a WispOrb once its owner has left every
    // Despise region) moved to DespiseRegion.OnExit instead, the per-region hook this
    // engine actually provides.
    public static void Configure()
    {
        EventSink.Connected += OnLogin;
    }

    public static void Initialize()
    {
        if (Instance != null)
        {
            CommandSystem.Register("CheckSpawnersVersion3", AccessLevel.Administrator, e => Instance.CheckSpawnersVersion3());
        }
    }

    public static DespiseController Instance { get; set; }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            this.MarkDirty();

            if (_enabled)
            {
                BeginTimer();
            }
            else
            {
                EndTimer();
            }
        }
    }

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextBossEncounter;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DespiseBoss _boss;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _deadLine;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Alignment _sequenceAlignment;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Sequencing { get; private set; }

    private bool _playersInSequence;

    private TimerExecutionToken _timer;
    private TimerExecutionToken _sequenceTimer;
    private bool _sequenceTimerActive;
    private TimerExecutionToken _cleanupTimer;
    private bool _cleanupTimerActive;

    private DespiseRegion _goodRegion;
    private DespiseRegion _evilRegion;
    private DespiseRegion _lowerRegion;
    private DespiseRegion _startRegion;

    public Region GoodRegion => _goodRegion;
    public Region EvilRegion => _evilRegion;
    public Region LowerRegion => _lowerRegion;
    public Region StartRegion => _startRegion;

    private readonly List<DespiseCreature> _evilArmy = new();
    private readonly List<DespiseCreature> _goodArmy = new();

    public List<DespiseCreature> EvilArmy => _evilArmy;
    public List<DespiseCreature> GoodArmy => _goodArmy;

    private readonly List<Mobile> _toTransport = new();

    private static readonly TimeSpan EncounterCheckDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DeadLineDuration = TimeSpan.FromMinutes(90);

    public bool IsInSequence => _sequenceTimerActive || _cleanupTimerActive;

    [Constructible]
    public DespiseController() : base(3806)
    {
        Movable = false;
        Visible = false;

        _enabled = true;
        Instance = this;

        _nextBossEncounter = DateTime.UtcNow;
        _boss = null;

        if (_enabled)
        {
            BeginTimer();
        }

        CreateSpawners();
    }

    public static WispOrb GetWispOrb(Mobile from) => WispOrb.Orbs.FirstOrDefault(orb => orb is { Deleted: false } && orb.Owner == from);

    private void BeginTimer()
    {
        EndTimer();

        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnTick, out _timer);

        _lowerRegion = new DespiseRegion("Despise Lower", LowerLevelBounds, true);
        _evilRegion = new DespiseRegion("Despise Evil", EvilBounds);
        _goodRegion = new DespiseRegion("Despise Good", GoodBounds);
        _startRegion = new DespiseRegion("Despise Start", new[] { new Rectangle2D(5568, 623, 22, 20) });
    }

    private void EndTimer()
    {
        _timer.Cancel();

        _lowerRegion?.Unregister();
        _evilRegion?.Unregister();
        _goodRegion?.Unregister();
        _startRegion?.Unregister();

        _lowerRegion = null;
        _evilRegion = null;
        _goodRegion = null;
        _startRegion = null;
    }

    /// <summary>Neither ServUO's original nor the first pass of this port had a delete hook,
    /// so `[DeleteDespise` (DespiseSetup.cs) dropped the controller item while its 1-minute
    /// OnTick timer kept firing and all four DespiseRegions stayed registered — a second
    /// `[SetupDespise` then stacked four more on the same bounds. Everything BeginTimer()
    /// puts up has to come back down here.</summary>
    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        EndTimer();
        EndSequenceTimer();
        EndCleanupTimer();

        Sequencing = false;
        _playersInSequence = false;

        if (_boss is { Deleted: false })
        {
            _boss.Delete();
        }

        _boss = null;

        _evilArmy.Clear();
        _goodArmy.Clear();
        _toTransport.Clear();
        _goodSpawners = null;
        _evilSpawners = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnTick()
    {
        if (_nextBossEncounter == DateTime.MinValue || _nextBossEncounter > DateTime.UtcNow)
        {
            return;
        }

        var good = GetArmyPower(Alignment.Good);
        var evil = GetArmyPower(Alignment.Evil);
        var strongest = Alignment.Neutral;

        if (good == 0 && evil == 0)
        {
            _nextBossEncounter = DateTime.UtcNow + EncounterCheckDuration;
        }
        else if (good > evil)
        {
            strongest = Alignment.Good;
        }
        else if (good < evil)
        {
            strongest = Alignment.Evil;
        }
        else
        {
            strongest = 0.5 > Utility.RandomDouble() ? Alignment.Good : Alignment.Evil;
        }

        var players = new List<Mobile>();
        players.AddRange(_goodRegion.GetPlayers());
        players.AddRange(_evilRegion.GetPlayers());
        players.AddRange(_startRegion.GetPlayers());

        foreach (var m in players)
        {
            if (!m.Player)
            {
                continue;
            }

            var orb = GetWispOrb(m);
            m.PlaySound(0x66C);

            if (orb == null || orb.Alignment != strongest)
            {
                // The Call to Arms has sounded, but your forces are not yet strong enough to heed it. /
                // Your enemy forces are stronger, and they have been called to battle.
                m.SendLocalizedMessage(strongest != Alignment.Neutral ? 1153334 : 1153333);
            }
            else
            {
                m.SendLocalizedMessage(1153332); // The Call to Arms has sounded. The forces of your alignment are strong, and you have been called to battle!

                if (orb.Conscripted)
                {
                    m.SendLocalizedMessage(1153337); // You will be teleported into the depths of the dungeon within 60 seconds...
                    _toTransport.Add(m);
                }
                else
                {
                    m.SendLocalizedMessage(1153338); // You have under 60 seconds to conscript a creature...
                }
            }
        }

        if (strongest != Alignment.Neutral)
        {
            _sequenceAlignment = strongest;

            Timer.DelayCall(TimeSpan.FromSeconds(60), BeginSequence);
            _nextBossEncounter = DateTime.MinValue;
            Sequencing = true;
        }
    }

    public int GetArmyPower(Alignment alignment) =>
        WispOrb.Orbs.Where(orb => orb.Conscripted && orb.Alignment == alignment).Sum(orb => orb.GetArmyPower());

    public void TryAddToArmy(WispOrb orb)
    {
        if (orb.Owner != null && Sequencing && orb.Alignment == _sequenceAlignment && !_toTransport.Contains(orb.Owner))
        {
            _toTransport.Add(orb.Owner);
        }
    }

    #region Spawner Stuff

    private List<XmlSpawner> _goodSpawners;
    private List<XmlSpawner> _evilSpawners;

    [CommandProperty(AccessLevel.GameMaster)]
    public int GoodSpawnerCount => _goodSpawners?.Count ?? 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public int EvilSpawnerCount => _evilSpawners?.Count ?? 0;

    private void CreateSpawners()
    {
        _goodSpawners = new List<XmlSpawner>();
        _evilSpawners = new List<XmlSpawner>();

        if (_lowerRegion == null)
        {
            return;
        }

        foreach (var item in _lowerRegion.GetItems())
        {
            if (item is XmlSpawner spawner && spawner.Name != null && spawner.Name.ToLower().Contains("despiserevamped"))
            {
                if (spawner.Name.ToLower().Contains("despiserevamped good"))
                {
                    _goodSpawners.Add(spawner);
                }

                if (spawner.Name.ToLower().Contains("despiserevamped evil"))
                {
                    _evilSpawners.Add(spawner);
                }
            }
        }
    }

    private void ResetSpawners(bool reset)
    {
        if (reset)
        {
            foreach (var spawner in _evilSpawners.Where(s => s.Running))
            {
                spawner.DoReset = true;
            }

            foreach (var spawner in _goodSpawners.Where(s => s.Running))
            {
                spawner.DoReset = true;
            }
        }
        else
        {
            var useList = _sequenceAlignment == Alignment.Good ? _evilSpawners : _goodSpawners;

            if (useList == null)
            {
                return;
            }

            foreach (var spawner in useList)
            {
                spawner.DoRespawn = true;
            }
        }
    }

    #endregion

    #region Instance Sequence

    private void BeginSequence()
    {
        Sequencing = false;

        if (_toTransport.Count == 0)
        {
            _nextBossEncounter = DateTime.UtcNow + EncounterCheckDuration;
            _sequenceAlignment = Alignment.Neutral;
            return;
        }

        _boss = _sequenceAlignment == Alignment.Good ? new AndrosTheDreadLord() : new AdrianTheGloriousLord();

        ResetSpawners(false);

        _boss.MoveToWorld(BossLocation, Map.Trammel);
        _deadLine = DateTime.UtcNow + DeadLineDuration;

        BeginSequenceTimer();
        KickFromBossRegion();

        Timer.DelayCall(TimeSpan.FromSeconds(60), TransportPlayers);

        // You have been called to assist in a fight of good versus evil... / The Overlord is
        // shielded from all attacks by players... / The Lieutenants are vulnerable...
        Timer.DelayCall(TimeSpan.FromSeconds(12), () => SendReadyMessage(1153339));
        Timer.DelayCall(TimeSpan.FromSeconds(24), () => SendReadyMessage(1153340));
        Timer.DelayCall(TimeSpan.FromSeconds(36), () => SendReadyMessage(1153341));
    }

    private void EndSequence()
    {
        if (_boss is { Deleted: false })
        {
            _boss.Delete();
        }

        _boss = null;
        _playersInSequence = false;
        EndCleanupTimer();
        KickFromBossRegion();
        _sequenceAlignment = Alignment.Neutral;

        _deadLine = DateTime.MinValue;
        _toTransport.Clear();

        Timer.DelayCall(TimeSpan.FromSeconds(10), () => ResetSpawners(true));

        _nextBossEncounter = DateTime.UtcNow + EncounterCheckDuration;
    }

    private void OnSequenceTick()
    {
        if (_sequenceTimerActive && _deadLine < DateTime.UtcNow && _lowerRegion != null)
        {
            EndSequenceTimer();
            SendRegionMessage(_lowerRegion, 1153348); // You were unable to defeat the enemy overlord in the time allotted...

            Timer.DelayCall(TimeSpan.FromSeconds(1), EndSequence);
        }
        else if (_playersInSequence && !HasPlayers(_lowerRegion))
        {
            EndSequenceTimer();
            Timer.DelayCall(TimeSpan.FromSeconds(1), EndSequence);
        }
    }

    public void OnBossSlain()
    {
        EndSequenceTimer();
        SendRegionMessage(_lowerRegion, 1153343); // The battle has ended...

        BeginCleanupTimer();
    }

    private static void SendRegionMessage(DespiseRegion region, int cliloc)
    {
        if (region == null)
        {
            return;
        }

        foreach (var m in region.GetMobiles())
        {
            if (m is PlayerMobile)
            {
                m.SendLocalizedMessage(cliloc);
            }
        }
    }

    private void KickFromBossRegion()
    {
        if (_lowerRegion == null)
        {
            return;
        }

        var bounds = _sequenceAlignment == Alignment.Evil ? EvilKickBounds : GoodKickBounds;

        foreach (var m in _lowerRegion.GetPlayers())
        {
            var orb = GetWispOrb(m);
            var p = GetRandomLoc(bounds);

            m.MoveToWorld(p, Map.Trammel);

            if (orb is { Pet: { Alive: true } })
            {
                orb.Pet.MoveToWorld(p, Map.Trammel);
            }

            m.SendLocalizedMessage(1153346); // You are summoned back to your stronghold.
        }
    }

    private void TransportPlayers()
    {
        var list = new List<Mobile>(_toTransport);

        foreach (var m in list)
        {
            var orb = GetWispOrb(m);

            if (orb is not { Deleted: false, Conscripted: true } || m.Region?.IsPartOf<DespiseRegion>() != true)
            {
                _toTransport.Remove(m);
            }
        }

        if (_toTransport.Count == 0)
        {
            EndSequenceTimer();
            EndSequence();
        }
        else
        {
            foreach (var m in _toTransport)
            {
                if (m?.Region == null || !m.Region.IsPartOf<DespiseRegion>())
                {
                    continue;
                }

                var orb = GetWispOrb(m);

                if (orb is { Pet: { Alive: true } })
                {
                    var p = GetRandomLoc(BossEntranceLocation);
                    m.MoveToWorld(p, Map.Trammel);
                    orb.Pet.MoveToWorld(p, Map.Trammel);

                    m.SendLocalizedMessage(1153280, "You!");
                    orb.Anchor = m;
                    orb.Pet.ControlTarget = m;
                    orb.Pet.ControlOrder = OrderType.Follow;
                }
            }

            _playersInSequence = true;
        }
    }

    public static bool HasPlayers(Region r) => r != null && r.GetPlayerCount() > 0;

    private static Point3D GetRandomLoc(Rectangle2D rec)
    {
        var map = Map.Trammel;
        var p = new Point3D(rec.X, rec.Y, map.GetAverageZ(rec.X, rec.Y));

        for (var i = 0; i < 50; i++)
        {
            var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width);
            var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height);
            var z = map.GetAverageZ(x, y);

            if (map.CanSpawnMobile(x, y, z))
            {
                p = new Point3D(x, y, z);
                break;
            }
        }

        return p;
    }

    public void BeginSequenceTimer()
    {
        EndSequenceTimer();
        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnSequenceTick, out _sequenceTimer);
        _sequenceTimerActive = true;
    }

    public void EndSequenceTimer()
    {
        _sequenceTimer.Cancel();
        _sequenceTimerActive = false;
    }

    public void BeginCleanupTimer()
    {
        EndCleanupTimer();
        Timer.StartTimer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), EndSequence, out _cleanupTimer);
        _cleanupTimerActive = true;

        foreach (var m in _lowerRegion.GetMobiles())
        {
            if (m is DespiseCreature { Orb: not null })
            {
                m.Delete();
            }
        }
    }

    public void EndCleanupTimer()
    {
        _cleanupTimer.Cancel();
        _cleanupTimerActive = false;
    }

    public static void OnLogin(Mobile from)
    {
        var controller = Instance;

        if (controller?.LowerRegion == null)
        {
            return;
        }

        if (from.Region == null || !from.Region.IsPartOf(controller.LowerRegion) || controller.IsInSequence)
        {
            return;
        }

        var orb = GetWispOrb(from);
        var bounds = orb is { Alignment: Alignment.Good } ? GoodKickBounds : EvilKickBounds;

        while (true)
        {
            var x = Utility.RandomMinMax(bounds.X, bounds.X + bounds.Width);
            var y = Utility.RandomMinMax(bounds.Y, bounds.Y + bounds.Height);
            var z = Map.Trammel.GetAverageZ(x, y);

            if (!Map.Trammel.CanSpawnMobile(x, y, z))
            {
                continue;
            }

            from.MoveToWorld(new Point3D(x, y, z), Map.Trammel);

            if (orb is { Pet: { Alive: true } })
            {
                orb.Pet.MoveToWorld(new Point3D(x, y, z), Map.Trammel);
            }

            break;
        }
    }

    /// <summary>Was EventSink.OnEnterRegion, a global "entered some region" event this
    /// codebase doesn't have — called from DespiseRegion.OnExit instead (the per-region
    /// hook that does exist), checked at the moment of leaving any of the 4 Despise
    /// sub-regions to see if the player has left Despise entirely.</summary>
    public static void CheckOrbStillInRegion(Mobile m)
    {
        var orb = GetWispOrb(m);

        if (orb != null && !Region.Find(m.Location, m.Map).IsPartOf<DespiseRegion>())
        {
            Timer.DelayCall(() =>
            {
                m.SendLocalizedMessage(1153233); // The Wisp Orb vanishes to whence it came...
                orb.Delete();
            });
        }
    }

    private void SendReadyMessage(int cliloc)
    {
        foreach (var m in _toTransport)
        {
            m.SendLocalizedMessage(cliloc);
        }
    }

    #endregion

    #region Location Defs

    public static readonly Rectangle2D[] EvilBounds = { new(5381, 644, 149, 120) };
    public static readonly Rectangle2D[] GoodBounds = { new(5380, 515, 134, 121) };
    public static readonly Rectangle2D[] LowerLevelBounds = { new(5379, 771, 247, 250) };

    private static readonly Rectangle2D EvilKickBounds = new(5500, 571, 20, 5);
    private static readonly Rectangle2D GoodKickBounds = new(5484, 567, 15, 8);
    private static readonly Rectangle2D BossEntranceLocation = new(5391, 855, 13, 15);

    private static readonly Point3D BossLocation = new(5556, 823, 45);

    #endregion

    public static void RemoveAnkh()
    {
        // Та же ловушка, что и в подмене телепортов при установке: Delete прямо в цикле
        // рушит перечислитель сектора. Здесь она до поры не срабатывала только потому,
        // что удалять обычно нечего — но при повторной установке компоненты анха уже
        // стоят, и обход падал бы на первом же.
        var stale = new List<Item>();

        foreach (var item in Map.Trammel.GetItemsInRange(new Point3D(5474, 525, 79), 3))
        {
            if (item is RejuvinationAddonComponent && !item.Deleted)
            {
                stale.Add(item);
            }
        }

        foreach (var item in stale)
        {
            item.Delete();
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _goodSpawners = new List<XmlSpawner>();
        _evilSpawners = new List<XmlSpawner>();
        Instance = this;

        Timer.DelayCall(CreateSpawners);

        if (!_enabled)
        {
            return;
        }

        BeginTimer();

        if (_deadLine > DateTime.UtcNow)
        {
            if (_boss is { Alive: true })
            {
                BeginSequenceTimer();
                return;
            }
        }
        else if (_deadLine != DateTime.MinValue)
        {
            BeginCleanupTimer();
            return;
        }

        Timer.DelayCall(EndSequence);
    }

    public void CheckSpawnersVersion3()
    {
        foreach (var spawner in World.Items.Values.OfType<XmlSpawner>()
                     .Where(s => s.Name != null && s.Name.ToLower().Contains("despiserevamped")))
        {
            foreach (var obj in spawner.SpawnObjects)
            {
                if (obj.TypeName != null)
                {
                    if (obj.TypeName.ToLower().Contains("berlingblades"))
                    {
                        obj.TypeName = obj.TypeName.Replace("BerlingBlades", "BirlingBlades");
                    }
                    else if (obj.TypeName.ToLower().Contains("sagittari") && !obj.TypeName.ToLower().Contains("sagittarri"))
                    {
                        obj.TypeName = obj.TypeName.Replace("Sagittari", "Sagittarri");
                    }
                }

                // Проверка на null стоит на десять строк выше, а здесь TypeName
                // разыменовывался без неё — а пустые слоты у XmlSpawner дело обычное.
                // Отсюда и NullReferenceException в консоли при [SetupDespise.
                if (obj.TypeName != null &&
                    (Region.Find(spawner.Location, spawner.Map) == _goodRegion ||
                     Region.Find(spawner.Location, spawner.Map) == _evilRegion)
                    && !obj.TypeName.Contains(@",{RND,1,5}"))
                {
                    obj.TypeName += @",{RND,1,5}";
                }
            }
        }

        foreach (var r in new Region[] { _goodRegion, _evilRegion, _lowerRegion, _startRegion })
        {
            if (r == null)
            {
                continue;
            }

            foreach (var item in r.GetItems().Where(i => i is Moongate or GateTeleporter).ToList())
            {
                item.Delete();
            }
        }

        DespiseRevampedSetup.SetupTeleporters();
    }
}
