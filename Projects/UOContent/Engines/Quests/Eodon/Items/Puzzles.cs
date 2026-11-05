using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     Puzzle objects of the "Valley of One" Eodon line, ported from ServUO
///     (Scripts/Quests/Eodon/Valley of One Quest/DragonTurtleNest.cs, TigerCubEnclosure.cs, Volcano.cs,
///     MyrmidexSpawner.cs).
///
///     Dropped, because the MLQuest engine cannot be driven from an item:
///     <list type="bullet">
///         <item><c>EmptyNestQuest.Update</c> / <c>PrideOfTheAmbushQuest.Update</c> calls — the quests count kills instead.</item>
///         <item><c>VolcanoRegion.CheckTravel</c> (teleport-only) — the travel check enum differs here; the region only
///         tracks movement.</item>
///     </list>
///     <c>EodonTribeRegion</c> replaces ServUO's <c>Region.GetItemCount(predicate)</c> with a pass over
///     <see cref="Region.GetItems" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonTurtleNest : BaseAddon
{
    [SerializableField(0)]
    private Mobile _hatchling;

    private List<BaseCreature> _poachers;
    private Mobile _focus;
    private DateTime _cooldownEnds;
    private bool _isHatching;
    private TimerExecutionToken _deadlineToken;

    [Constructible]
    public DragonTurtleNest()
    {
        var comp = new LocalizedAddonComponent(3518, 1026869) { Hue = 449 };
        AddComponent(comp, 0, 0, 0);

        comp = new LocalizedAddonComponent(3270, 1026869) { Hue = 551 };
        AddComponent(comp, 0, 0, 2);

        comp = new LocalizedAddonComponent(3374, 1026869) { Hue = 551 };
        AddComponent(comp, 0, 0, 2);

        AddComponent(new LocalizedAddonComponent(16831, 1112469), 1, 1, 15);
    }

    public TimeSpan ResetPeriod => TimeSpan.FromSeconds(Utility.RandomMinMax(120, 360));

    public bool IsInCooldown => _cooldownEnds != DateTime.MinValue && Core.Now < _cooldownEnds;

    public override bool HandlesOnMovement => !IsInCooldown;

    private AddonComponent Egg
    {
        get
        {
            foreach (var component in Components)
            {
                if (component.ItemID == 16831)
                {
                    return component;
                }
            }

            return null;
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (m is not PlayerMobile pm || m.Location == oldLocation || !m.InRange(Location, 3))
        {
            return;
        }

        if (MLQuestSystem.GetContext(pm)?.IsDoingQuest(typeof(EmptyNestQuest)) != true)
        {
            return;
        }

        if (_focus == null)
        {
            _focus = m;
            m.RevealingAction();
            SpawnPoachers(m);

            Timer.StartTimer(
                TimeSpan.FromMinutes(5),
                () =>
                {
                    _deadlineToken.Cancel();

                    if (_poachers?.Count > 0)
                    {
                        Delete();
                    }
                    else
                    {
                        _focus = null;
                        Hatchling = null;
                        _cooldownEnds = DateTime.MinValue;
                    }
                },
                out _deadlineToken
            );
        }
        else if (IsInCooldown)
        {
            _cooldownEnds = Core.Now + TimeSpan.FromMinutes(2);
        }
    }

    public void SpawnPoachers(Mobile m)
    {
        var map = m.Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        _poachers ??= new List<BaseCreature>();

        for (var i = 0; i < 3; i++)
        {
            var poacher = new Poacher(this);
            var point = m.Location;

            for (var j = 0; j < 10; j++)
            {
                var x = Utility.RandomMinMax(m.X - 2, m.X + 2);
                var y = Utility.RandomMinMax(m.Y - 2, m.Y + 2);
                var z = map.GetAverageZ(x, y);

                if (map.CanSpawnMobile(x, y, z))
                {
                    point = new Point3D(x, y, z);
                    break;
                }
            }

            poacher.MoveToWorld(point, map);
            _poachers.Add(poacher);
        }

        foreach (var poacher in _poachers)
        {
            poacher.Combatant = m;
        }
    }

    public void OnPoacherKilled(BaseCreature creature)
    {
        if (_poachers == null || !_poachers.Contains(creature))
        {
            return;
        }

        _poachers.Remove(creature);

        if (_poachers.Count > 0)
        {
            return;
        }

        _poachers = null;

        if (_isHatching)
        {
            if (Hatchling?.Alive == true)
            {
                _cooldownEnds = Core.Now + ResetPeriod;
                Timer.StartTimer(TimeSpan.FromSeconds(1), OnComplete);
            }
            else
            {
                Delete();
            }

            _deadlineToken.Cancel();
            Hatchling = null;
            _focus = null;
            return;
        }

        _isHatching = true;

        Timer.StartTimer(
            TimeSpan.FromSeconds(Utility.RandomMinMax(15, 30)),
            () =>
            {
                var hatchling = new DragonTurtleHatchling { Tamable = false };
                hatchling.MoveToWorld(Location, Map);

                Hatchling = hatchling;

                SpawnPoachers(hatchling);

                var egg = Egg;

                if (egg != null)
                {
                    egg.Visible = false;
                }
            }
        );
    }

    private void OnComplete()
    {
        var hatchling = Hatchling;
        var focus = _focus;

        if (hatchling != null)
        {
            // *The Hatchling safely burrows into the sand*
            focus?.PublicOverheadMessage(MessageType.Regular, 0x35, 1156496);
            Timer.StartTimer(TimeSpan.FromSeconds(1), hatchling.Delete);
        }

        Timer.StartTimer(
            TimeSpan.FromMinutes(1),
            () =>
            {
                var egg = Egg;

                if (egg?.Visible == false)
                {
                    egg.Visible = true;
                }
            }
        );

        _isHatching = false;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _deadlineToken.Cancel();

        if (_poachers != null)
        {
            foreach (var poacher in _poachers)
            {
                poacher?.Delete();
            }

            _poachers = null;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CubEnclosure : BaseAddon
{
    public static readonly Point3D HomeLocation = new(354, 1894, 2);

    [SerializableField(0)]
    private TigerCub _cub;

    [Constructible]
    public CubEnclosure()
    {
        AddComponent(new AddonComponent(2103), 0, 0, 0);
        AddComponent(new AddonComponent(2103), 0, 1, 0);
        AddComponent(new AddonComponent(2103), 0, 2, 0);
        AddComponent(new AddonComponent(2102), 1, 2, 0);
        AddComponent(new AddonComponent(2101), 3, 2, 0);
        AddComponent(new AddonComponent(2103), 3, 1, 0);
        AddComponent(new AddonComponent(2103), 3, 0, 0);
        AddComponent(new AddonComponent(2102), 2, -1, 0);
        AddComponent(new AddonComponent(2102), 1, -1, 0);
        AddComponent(new AddonComponent(2102), 3, -1, 0);

        AddComponent(new EnclosureDoor(), 2, 2, 0);

        Cub = new TigerCub { Blessed = true, RangeHome = 5 };
    }

    public override BaseAddonDeed Deed => null;

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        if (Cub?.Deleted == false)
        {
            Cub.MoveToWorld(new Point3D(X + 1, Y + 1, Z), Map);
            Cub.Home = Cub.Location;
        }
    }

    public EnclosureDoor Door
    {
        get
        {
            foreach (var component in Components)
            {
                if (component is EnclosureDoor door)
                {
                    return door;
                }
            }

            return null;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Cub != null && !Cub.Deleted)
        {
            Cub.MoveToWorld(new Point3D(X + 1, Y + 1, Z), Map);
            Cub.Home = Cub.Location;
        }
    }

    public void OnPuzzleCompleted(PlayerMobile pm)
    {
        // *You watch as the Tiger Cub safely returns to the Kurak Tribe*
        pm.PrivateOverheadMessage(MessageType.Regular, 0x35, 1156501, pm.NetState);

        if (Cub != null)
        {
            Cub.Blessed = false;
            Cub.Protector = pm;
            Cub.Home = HomeLocation;
            Cub.RangeHome = 1;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(0.25), Delete);
    }

    public void OnBadDirection(PlayerMobile pm)
    {
        if (Utility.RandomBool())
        {
            // *A poisonous dart embeds itself into your neck!*
            pm.PrivateOverheadMessage(MessageType.Regular, 0x35, 1156493, pm.NetState);
            Effects.PlaySound(pm.Location, pm.Map, 0x22E);
            pm.Damage(Utility.RandomMinMax(20, 40));
            pm.ApplyPoison(pm, Poison.Deadly);
            return;
        }

        // *You are ambushed by attacking trappers!*
        pm.PrivateOverheadMessage(MessageType.Regular, 0x35, 1156494, pm.NetState);

        SpawnTrappers(pm);
    }

    private void SpawnTrappers(Mobile m)
    {
        if (m?.Alive != true)
        {
            return;
        }

        for (var i = 0; i < 3; i++)
        {
            var point = m.Location;

            for (var j = 0; j < 25; j++)
            {
                var x = Utility.RandomMinMax(m.X - 2, m.X + 2);
                var y = Utility.RandomMinMax(m.Y - 2, m.Y + 2);
                var z = m.Map.GetAverageZ(x, y);

                if (m.Map.CanSpawnMobile(x, y, z))
                {
                    point = new Point3D(x, y, z);
                    break;
                }
            }

            var trapper = new Trapper();
            trapper.MoveToWorld(point, m.Map);

            Timer.StartTimer(TimeSpan.FromSeconds(0.25), () => trapper.Combatant = m);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (Cub?.Protector == null)
        {
            Cub?.Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class EnclosureDoor : AddonComponent
{
    private static readonly int[][] Paths =
    [
        [0, 1, 2, 3, 7, 11, 15],
        [0, 4, 8, 12, 13, 9, 5, 1, 2, 6, 10, 14, 15],
        [0, 4, 5, 1, 2, 3, 7, 6, 10, 14, 15],
        [0, 4, 8, 12, 13, 14, 10, 6, 2, 3, 7, 11, 15],
        [0, 4, 5, 9, 13, 14, 10, 11, 15],
        [0, 4, 5, 1, 2, 6, 10, 9, 13, 14, 15],
        [0, 1, 5, 4, 8, 12, 13, 14, 10, 11, 15],
        [0, 1, 2, 6, 5, 4, 8, 12, 13, 9, 10, 11, 15],
        [0, 1, 5, 6, 7, 11, 10, 9, 13, 14, 15],
        [0, 1, 5, 6, 7, 11, 15],
        [0, 1, 5, 6, 10, 9, 13, 14, 15],
        [0, 1, 2, 3, 7, 6, 5, 4, 8, 9, 1, 11, 12, 13, 14, 15],
        [0, 4, 8, 12, 13, 14, 15],
        [0, 4, 5, 9, 8, 12, 13, 14, 10, 6, 2, 3, 7, 11, 15]
    ];

    [Constructible]
    public EnclosureDoor() : base(2086)
    {
        Movable = false;
        Path = GetRandomPath();
    }

    public List<int> Path { get; set; }

    [AfterDeserialization]
    private void AfterDeserialization() => Path = GetRandomPath();

    public static List<int> GetRandomPath() => new(Paths[Utility.Random(Paths.Length)]);

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile pm || !from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
        {
            return;
        }

        if (MLQuestSystem.GetContext(pm)?.IsDoingQuest(typeof(PrideOfTheAmbushQuest)) == true)
        {
            pm.SendGump(new LockingMechanismGump(pm, this));
        }
    }

    public class LockingMechanismGump : Gump
    {
        private readonly EnclosureDoor _door;
        private readonly PlayerMobile _user;
        private readonly List<int> _progress;
        private readonly bool _showNext;

        public LockingMechanismGump(PlayerMobile pm, EnclosureDoor door) : this(pm, door, [0], false)
        {
        }

        public LockingMechanismGump(PlayerMobile pm, EnclosureDoor door, List<int> progress, bool showNext) : base(25, 25)
        {
            _door = door;
            _user = pm;
            _progress = progress;
            _showNext = showNext;

            AddGumpLayout();
        }

        private List<int> Path => _door?.Deleted == false ? _door.Path : null;

        public void AddGumpLayout()
        {
            AddBackground(50, 50, 550, 440, 2600);
            AddBackground(100, 75, 450, 90, 2600);
            AddBackground(90, 175, 270, 270, 2600);
            AddBackground(100, 185, 250, 250, 5120);
            AddBackground(370, 175, 178, 200, 5120);

            AddImage(145, 95, 10451);
            AddImage(435, 95, 10451);
            AddImage(0, 50, 10400);
            AddImage(0, 200, 10401);
            AddImage(0, 360, 10402);

            AddImage(565, 50, 10410);
            AddImage(565, 200, 10411);
            AddImage(565, 360, 10412);

            AddImage(370, 175, 10452);
            AddImage(370, 360, 10452);

            AddImageTiled(125, 207, 130, 3, 5031);
            AddImageTiled(125, 247, 130, 3, 5031);
            AddImageTiled(125, 287, 130, 3, 5031);
            AddImageTiled(125, 327, 130, 3, 5031);

            AddImageTiled(123, 205, 3, 130, 5031);
            AddImageTiled(163, 205, 3, 130, 5031);
            AddImageTiled(203, 205, 3, 130, 5031);
            AddImageTiled(243, 205, 3, 130, 5031);

            AddImage(420, 250, 1417);
            AddImage(440, 270, 2642);

            AddHtmlLocalized(220, 90, 210, 16, 1156490, false, false);  // <center>Enclosure Locking Mechanism</center>
            AddHtmlLocalized(220, 112, 210, 16, 1153748, false, false); // <center>Use the Directional Controls to</center>
            AddHtmlLocalized(220, 131, 210, 16, 1156491, false, false); // <center>Unlock the Enclosure</center>

            const int x = 110;
            const int y = 195;

            for (var i = 0; i < 16; i++)
            {
                var itemID = _progress.Contains(i) ? 2152 : 9720;

                var xOffset = x + 40 * (i % 4);
                var yOffset = y + 40 * (i / 4);

                AddImage(xOffset, yOffset, itemID);

                if (i == _progress[^1])
                {
                    AddImage(xOffset + 8, yOffset + 8, 5032);
                }

                if (_showNext && _progress.Count < Path?.Count && i == Path[_progress.Count])
                {
                    AddImage(xOffset + 8, yOffset + 8, 2361);
                }
            }

            if (_user.Skills[SkillName.Lockpicking].Base >= 100)
            {
                AddHtmlLocalized(410, 415, 150, 32, 1156492, false, false); // Attempt to pick the enclosure lock
                AddButton(370, 415, 4005, 4005, 5, GumpButtonType.Reply, 0);
            }

            AddButton(453, 245, 10700, 10701, 1, GumpButtonType.Reply, 0); // up
            AddButton(478, 281, 10710, 10711, 2, GumpButtonType.Reply, 0); // right
            AddButton(453, 305, 10720, 10721, 3, GumpButtonType.Reply, 0); // down
            AddButton(413, 281, 10730, 10731, 4, GumpButtonType.Reply, 0); // left
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID is <= 0 or > 5)
            {
                return;
            }

            if (_door?.Deleted != false || Path == null)
            {
                return;
            }

            if (info.ButtonID == 5)
            {
                _user.SendGump(new LockingMechanismGump(_user, _door, _progress, true));
                return;
            }

            HandleButton(info.ButtonID);
        }

        private void HandleButton(int id)
        {
            var current = _progress[^1];

            var next = _progress.Count >= 0 && _progress.Count < Path.Count ? Path[_progress.Count] : 15;

            var pick = id switch
            {
                1 => current - 4,
                2 => current + 1,
                3 => current + 4,
                _ => current - 1
            };

            if (_progress.Contains(pick) || pick is < 0 or > 15)
            {
                _user.PlaySound(0x5B6);
                _user.SendGump(new LockingMechanismGump(_user, _door, _progress, false));
                return;
            }

            if (current is 10 or 11 or 14 && pick == 15)
            {
                _user.PlaySound(0x3D);
                (_door.Addon as CubEnclosure)?.OnPuzzleCompleted(_user);
                return;
            }

            if (pick == next)
            {
                _progress.Add(pick);
                _user.PlaySound(0x1F5);
                _user.SendGump(new LockingMechanismGump(_user, _door, _progress, false));
                return;
            }

            (_door.Addon as CubEnclosure)?.OnBadDirection(_user);

            _progress.Clear();
            _progress.Add(0);

            _user.SendGump(new LockingMechanismGump(_user, _door, _progress, false));
        }
    }
}

public class Volcano
{
    private static readonly Rectangle2D LavaStart = new(927, 1615, 2, 2);
    private const int LastLavaStage = 70;

    private static readonly Rectangle2D[] SafeZone =
    [
        new(959, 1704, 15, 14), new(915, 1696, 15, 15), new(1056, 1584, 17, 19), new(884, 1695, 13, 15),
        new(1009, 1663, 13, 13), new(994, 1526, 15, 15), new(1012, 1541, 15, 14), new(1032, 1520, 19, 18),
        new(1011, 1601, 9, 9), new(897, 1554, 15, 15), new(834, 1546, 23, 20), new(837, 1675, 11, 14),
        new(838, 1636, 15, 12), new(856, 1697, 19, 18), new(879, 1668, 15, 19), new(991, 1590, 15, 15)
    ];

    private VolcanoRegion _region;
    private Rectangle2D _currentLava;
    private Rectangle2D _currentSafeZone;
    private int _lavaStage;
    private bool _erupting;
    private long _nextLavaAdvance;
    private DateTime _nextLava;
    private TimerExecutionToken _tickToken;
    private TimerExecutionToken _lavaToken;

    private Volcano()
    {
    }

    public static Volcano Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        VolcanoRegion.Initialize();

        Instance = new Volcano { _region = VolcanoRegion.Instance };
        Instance.Start();
    }

    private void Start() => Timer.StartTimer(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), OnTick, out _tickToken);

    private static bool InSafeZone(IPoint3D point)
    {
        var location = new Point3D(point.X, point.Y, 0);

        foreach (var rect in SafeZone)
        {
            if (rect.Contains(location))
            {
                return true;
            }
        }

        return false;
    }

    private void OnTick()
    {
        if (_region == null || _erupting)
        {
            return;
        }

        if (Core.Now < _nextLava)
        {
            return;
        }

        if (_region.GetPlayerCount() == 0)
        {
            _nextLava = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(20, 30));
            return;
        }

        foreach (var mobile in _region.GetPlayers())
        {
            if (mobile.AccessLevel == AccessLevel.Player)
            {
                // *The Volcano is becoming unstable!*
                mobile.PrivateOverheadMessage(MessageType.Regular, 0x21, 1156506, mobile.NetState);
            }
        }

        _currentLava = LavaStart;
        _currentSafeZone = _currentLava;
        _lavaStage = 0;
        _erupting = true;
        _nextLavaAdvance = Core.TickCount + 450;

        Timer.StartTimer(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250), OnLavaTick, out _lavaToken);
    }

    private void OnLavaTick()
    {
        if (!_erupting)
        {
            _lavaToken.Cancel();
            return;
        }

        if (Core.TickCount - _nextLavaAdvance < 0)
        {
            return;
        }

        _currentLava = new Rectangle2D(
            _currentLava.X - 2,
            _currentLava.Y - 2,
            _currentLava.Width + 4,
            _currentLava.Height + 4
        );
        _currentSafeZone = new Rectangle2D(
            _currentLava.X + 2,
            _currentLava.Y + 2,
            _currentLava.Width - 4,
            _currentLava.Height - 4
        );
        _nextLavaAdvance = Core.TickCount + 450;

        AddLava();
    }

    private bool CheckCoordinate(int x, int y) =>
        x <= _currentLava.X + 2 || x >= _currentLava.X + _currentLava.Width - 2 ||
        y <= _currentLava.Y + 2 || y >= _currentLava.Y + _currentLava.Height - 2;

    private void AddLava()
    {
        _lavaStage++;

        for (var x = _currentLava.X; x <= _currentLava.X + _currentLava.Width; x++)
        {
            for (var y = _currentLava.Y; y <= _currentLava.Y + _currentLava.Height; y++)
            {
                if (!CheckCoordinate(x, y))
                {
                    continue;
                }

                var point = new Point3D(x, y, 0);

                foreach (var mobile in Map.TerMur.GetMobilesInRange(point, 20))
                {
                    if (mobile is PlayerMobile && mobile.AccessLevel == AccessLevel.Player)
                    {
                        mobile.PlaySound(520);
                    }
                }

                if (!Map.TerMur.CanFit(x, y, 0, 16, false, false, true) || InSafeZone(point))
                {
                    continue;
                }

                Effects.SendLocationEffect(point, Map.TerMur, 4847, 20);

                foreach (var mobile in Map.TerMur.GetMobilesInRange(point, 0))
                {
                    if (mobile.Alive && mobile.AccessLevel == AccessLevel.Player &&
                        (mobile is PlayerMobile || mobile is BaseCreature { Controlled: true }))
                    {
                        DoLavaDamageDelayed(mobile);
                    }
                }
            }
        }

        if (_lavaStage < LastLavaStage)
        {
            return;
        }

        _lavaToken.Cancel();
        _erupting = false;
        _nextLava = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(20, 30));
    }

    public void CheckMovement(Mobile m)
    {
        if (!m.Alive || m.AccessLevel > AccessLevel.Player || m is BaseCreature { Controlled: false })
        {
            return;
        }

        if (_erupting && _currentLava.Contains(m.Location) && !_currentSafeZone.Contains(m.Location) &&
            !InSafeZone(m))
        {
            DoLavaDamageDelayed(m);
        }
    }

    private static void DoLavaDamage(Mobile m)
    {
        // *The flowing lava scorches you!*
        m.PrivateOverheadMessage(MessageType.Regular, 0x22, 1156497, m.NetState);
        AOS.Damage(m, null, Utility.RandomMinMax(200, 300), 0, 100, 0, 0, 0);
    }

    private static void DoLavaDamageDelayed(Mobile m) =>
        Timer.StartTimer(TimeSpan.FromSeconds(0.25), () => DoLavaDamage(m));
}

public class VolcanoRegion : Region
{
    public static VolcanoRegion Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new VolcanoRegion();
        Instance.Register();
    }

    private VolcanoRegion() : base("Eodon_Volcano", Map.TerMur, DefaultPriority, new Rectangle2D(832, 1502, 255, 217))
    {
    }

    public override void OnLocationChanged(Mobile m, Point3D oldLocation)
    {
        Volcano.Instance?.CheckMovement(m);

        base.OnLocationChanged(m, oldLocation);
    }
}

[SerializationGenerator(0, false)]
public partial class MyrmidexHill : Item
{
    private static readonly Type[] SpawnList =
    {
        typeof(MyrmidexLarvae), typeof(MyrmidexDrone), typeof(MyrmidexWarrior)
    };

    private List<BaseCreature> _spawn = new();
    private DateTime _nextSpawn;
    private int _hasSpawned;

    [Constructible]
    public MyrmidexHill() : this(null, null)
    {
    }

    public MyrmidexHill(EodonTribeRegion zone, Mobile focus) : base(8754)
    {
        Movable = false;

        Zone = zone;
        Focus = focus;
    }

    public EodonTribeRegion Zone { get; set; }

    public Mobile Focus { get; set; }

    public int SpawnCount => Utility.RandomMinMax(6, 9);

    public int HasSpawned
    {
        get => _hasSpawned;
        set => _hasSpawned = value;
    }

    public override bool HandlesOnMovement => _nextSpawn < Core.Now;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (!m.InRange(Location, 7) || m.AccessLevel != AccessLevel.Player)
        {
            return;
        }

        if (m is PlayerMobile || m is BaseCreature { Controlled: true })
        {
            Focus = m;
            DoSpawn();
        }
    }

    public void DoSpawn()
    {
        var map = Map;

        if (_spawn == null)
        {
            return;
        }

        for (var i = _spawn.Count - 1; i >= 0; i--)
        {
            var creature = _spawn[i];

            if (creature == null || !creature.Alive || creature.Deleted)
            {
                _spawn.RemoveAt(i);
            }
        }

        if (map == null || map == Map.Internal || Deleted || _spawn.Count != 0 || HasSpawned >= 3)
        {
            return;
        }

        HasSpawned++;
        _nextSpawn = Core.Now + TimeSpan.FromMinutes(Utility.RandomMinMax(2, 5));

        var time = 333;

        for (var i = 0; i < SpawnCount; i++)
        {
            Timer.StartTimer(
                TimeSpan.FromMilliseconds(time),
                () =>
                {
                    if (Deleted || Map == null || Map == Map.Internal)
                    {
                        return;
                    }

                    var point = Location;

                    for (var j = 0; j < 25; j++)
                    {
                        var x = Utility.RandomMinMax(X - 3, X + 3);
                        var y = Utility.RandomMinMax(Y - 3, Y + 3);
                        var z = map.GetAverageZ(x, y);

                        if (map.CanSpawnMobile(x, y, z))
                        {
                            point = new Point3D(x, y, z);
                            break;
                        }
                    }

                    if (SpawnList[Utility.Random(SpawnList.Length)].CreateInstance<BaseCreature>() is not { } creature)
                    {
                        return;
                    }

                    _spawn.Add(creature);
                    creature.MoveToWorld(point, map);
                    creature.Combatant = Focus;
                }
            );

            time += 333;
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_spawn != null)
        {
            foreach (var creature in _spawn)
            {
                creature?.Delete();
            }

            _spawn = null;
        }
    }
}

public class EodonTribeRegion : Region
{
    private static EodonTribeRegion[] _zones;

    public static void Initialize()
    {
        if (_zones != null)
        {
            return;
        }

        _zones =
        [
            new EodonTribeRegion(EodonTribe.Jukari, 6, new Rectangle2D(640, 2046, 115, 115)),
            new EodonTribeRegion(EodonTribe.Kurak, 6, new Rectangle2D(291, 1817, 125, 90)),
            new EodonTribeRegion(
                EodonTribe.Barrab,
                5,
                new Rectangle2D(134, 1767, 33, 20),
                new Rectangle2D(142, 1786, 57, 80),
                new Rectangle2D(145, 1750, 20, 20)
            ),
            new EodonTribeRegion(EodonTribe.Barako, 5, new Rectangle2D(620, 1677, 95, 100)),
            new EodonTribeRegion(EodonTribe.Urali, 5, new Rectangle2D(320, 1551, 160, 72)),
            new EodonTribeRegion(EodonTribe.Sakkhra, 8, new Rectangle2D(482, 1375, 200, 200))
        ];
    }

    public EodonTribeRegion(EodonTribe tribe, int maxSpawns, params Rectangle2D[] area)
        : base($"{tribe} tribe", Map.TerMur, DefaultPriority, area)
    {
        Tribe = tribe;
        MaxSpawns = maxSpawns;

        Register();
    }

    public int MaxSpawns { get; }

    public EodonTribe Tribe { get; set; }

    public int Spawns
    {
        get
        {
            var count = 0;

            foreach (var item in GetItems())
            {
                if (item is MyrmidexHill)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public override void OnLocationChanged(Mobile m, Point3D oldLocation)
    {
        if (Tribe != EodonTribe.Barrab && Spawns < MaxSpawns && m.AccessLevel == AccessLevel.Player &&
            (m is PlayerMobile || m is BaseCreature { Controlled: true }) && Utility.RandomDouble() < 0.005)
        {
            var point = m.Location;

            for (var i = 0; i < 10; i++)
            {
                var x = Utility.RandomMinMax(point.X - 5, point.X + 5);
                var y = Utility.RandomMinMax(point.Y - 5, point.Y + 5);
                var z = Map.GetAverageZ(x, y);

                if (Map.CanFit(x, y, z, 16, false, false, true))
                {
                    point = new Point3D(x, y, z);
                    break;
                }
            }

            var hill = new MyrmidexHill(this, m);
            hill.MoveToWorld(point, Map);
            hill.DoSpawn();
        }

        base.OnLocationChanged(m, oldLocation);
    }
}
