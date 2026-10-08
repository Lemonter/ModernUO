using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Commands;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Shadowguard;

// Encounters/Table/Addons are persisted separately via ShadowguardPersistence (a
// GenericPersistence, same pattern as Systems/MahaonCities/CityControlSystem.cs) rather than as
// codegen-serialized fields on this Item — ShadowguardEncounter keeps ServUO's own hand-rolled
// Serialize/Deserialize shape instead of needing every encounter subclass converted to the
// codegen attribute system. Queue (the wait-list for a full instance) is intentionally NOT
// persisted — losing an in-progress queue position across a restart is an acceptable simplification.
[SerializationGenerator(0, false)]
public partial class ShadowguardController : Item
{
    public static readonly TimeSpan ReadyDuration =
        TimeSpan.FromSeconds(ServerConfiguration.GetSetting("shadowguard.readyDurationSeconds", 30));

    public static readonly bool RandomInstances =
        ServerConfiguration.GetSetting("shadowguard.randomizeInstances", false);

    public static ShadowguardController Instance { get; set; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _kickLocation;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Rectangle2D _lobby;

    public Dictionary<Mobile, EncounterType> Table { get; set; }
    public List<ShadowguardEncounter> Encounters { get; set; }
    public Dictionary<Mobile, EncounterType> Queue { get; private set; }
    public List<BaseAddon> Addons { get; set; }
    public List<ShadowguardInstance> Instances { get; private set; }

    private Timer _timer;

    public override int LabelNumber => 1156235; // An Enchanting Crystal Ball

    private static ShadowguardPersistence _persistence;

    public static void Configure()
    {
        ShadowguardEncounter.Configure();
        _persistence = new ShadowguardPersistence();

        CommandSystem.Register("ShadowguardCompleteAllRooms", AccessLevel.GameMaster, CompleteAllRooms_OnCommand);
    }

    public static void Initialize()
    {
        EventSink.Connected += OnConnected;
        EventSink.Disconnected += OnDisconnected;
    }

    // No bespoke "AddShadowguardController" command — placed the same way as every other
    // world fixture in this codebase: `[add ShadowguardController` (generic AddCommand, via
    // the [Constructible] attribute below). The companion doors/ankh/landmark statics are
    // OSI-fixed-coordinate world furniture, not relative to wherever the GM targets the
    // controller itself, so they're spawned once alongside it in the constructor.

    [Usage("ShadowguardCompleteAllRooms")]
    [Description("Debug: marks every tower encounter complete for you, so you can attempt the Roof finale immediately.")]
    private static void CompleteAllRooms_OnCommand(CommandEventArgs e)
    {
        if (Instance == null)
        {
            e.Mobile.SendMessage("No Shadowguard controller exists yet.");
            return;
        }

        Instance.Table ??= new Dictionary<Mobile, EncounterType>();
        Instance.Table[e.Mobile] = EncounterType.Required;
    }

    public void InitializeInstances()
    {
        Instances = new List<ShadowguardInstance>();

        for (var i = 0; i < CenterPoints.Length; i++)
        {
            Instances.Add(new ShadowguardInstance(this, CenterPoints[i], EncounterBounds[i], i));
        }
    }

    [Constructible]
    public ShadowguardController() : base(0x468B)
    {
        var isFirst = Instance == null;
        Instance = this;

        _kickLocation = new Point3D(505, 2192, 25);
        _lobby = new Rectangle2D(497, 2153, 50, 80);

        Encounters = new List<ShadowguardEncounter>();
        Queue = new Dictionary<Mobile, EncounterType>();
        Addons = new List<BaseAddon>();

        InitializeInstances();

        Movable = false;

        StartTimer();

        if (isFirst)
        {
            PlaceWorldFixtures();
        }
    }

    // Doors/ankh/landmark statics from ServUO's Controller.cs: SetupShadowguard(Mobile from).
    private static void PlaceWorldFixtures()
    {
        var door = new MetalDoor(DoorFacing.NorthCCW) { Hue = 1779 };
        door.MoveToWorld(new Point3D(519, 2188, 25), Map.TerMur);

        door = new MetalDoor(DoorFacing.SouthCW) { Hue = 1779 };
        door.MoveToWorld(new Point3D(519, 2189, 25), Map.TerMur);

        door = new MetalDoor(DoorFacing.NorthCCW) { Hue = 1779 };
        door.MoveToWorld(new Point3D(519, 2192, 25), Map.TerMur);

        door = new MetalDoor(DoorFacing.SouthCW) { Hue = 1779 };
        door.MoveToWorld(new Point3D(519, 2193, 25), Map.TerMur);

        var ankh = new AnkhWest();
        ankh.MoveToWorld(new Point3D(503, 2191, 25), Map.TerMur);

        Item landmark = new Static(19343);
        landmark.MoveToWorld(new Point3D(64, 2336, 29), Map.TerMur);

        landmark = new Static(19343);
        landmark.MoveToWorld(new Point3D(160, 2336, 29), Map.TerMur);

        landmark = new Static(19343);
        landmark.MoveToWorld(new Point3D(64, 2432, 29), Map.TerMur);

        landmark = new Static(19343);
        landmark.MoveToWorld(new Point3D(160, 2432, 29), Map.TerMur);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 3))
        {
            from.SendGump(new ShadowguardGump(pm));
        }
    }

    public void OnTick()
    {
        if (Encounters == null)
        {
            return;
        }

        // Iterate a snapshot — Expire()/OnEncounterComplete() mutate Encounters mid-loop.
        foreach (var enc in Encounters.ToArray())
        {
            if (enc == null)
            {
                continue;
            }

            if (enc.EncounterDuration == TimeSpan.MaxValue)
            {
                enc.OnTick();
                continue;
            }

            var end = enc.StartTime + enc.EncounterDuration;

            if (!enc.DoneWarning && Core.Now > end - TimeSpan.FromMinutes(5))
            {
                enc.DoWarning();
            }
            else if (Core.Now >= end)
            {
                enc.Expire();
            }
            else
            {
                enc.OnTick();
            }
        }
    }

    public void CompleteRoof(Mobile m)
    {
        if (Table == null)
        {
            return;
        }

        Table.Remove(m);

        if (Table.Count == 0)
        {
            Table = null;
        }
    }

    public void OnEncounterComplete(ShadowguardEncounter encounter, bool expired)
    {
        Encounters.Remove(encounter);
        CheckQueue();

        if (expired)
        {
            return;
        }

        using var mobiles = encounter.Region.GetMobilesPooled();
        foreach (var m in mobiles)
        {
            if (m is PlayerMobile pm)
            {
                AddToTable(pm, encounter.Encounter);
            }
        }
    }

    public void AddToTable(Mobile m, EncounterType encounter)
    {
        if (encounter == EncounterType.Roof)
        {
            return;
        }

        Table ??= new Dictionary<Mobile, EncounterType>();

        if (Table.TryGetValue(m, out var existing))
        {
            Table[m] = existing | encounter;
        }
        else
        {
            Table[m] = encounter;
        }
    }

    public void AddEncounter(ShadowguardEncounter encounter) => Encounters.Add(encounter);

    public bool HasCompletedEncounter(Mobile m, EncounterType encounter) =>
        Table != null && Table.TryGetValue(m, out var completed) && (completed & encounter) != 0;

    public bool CanTryEncounter(Mobile m, EncounterType encounter)
    {
        var p = Party.Get(m);

        if (p != null && p.Leader != m)
        {
            // You may not start a Shadowguard encounter while in a party unless you are the party leader.
            m.SendLocalizedMessage(1156184);
            return false;
        }

        if (encounter == EncounterType.Roof)
        {
            if (p != null)
            {
                foreach (var info in p.Members)
                {
                    if (Table == null || !Table.TryGetValue(info.Mobile, out var completed) ||
                        (completed & EncounterType.Required) != EncounterType.Required)
                    {
                        // All members of your party must complete each of the Shadowguard Towers
                        // before attempting the finale.
                        m.SendLocalizedMessage(1156249);
                        return false;
                    }
                }
            }
            else if (Table == null || !Table.TryGetValue(m, out var completed) ||
                     (completed & EncounterType.Required) != EncounterType.Required)
            {
                // You must complete each level of Shadowguard before attempting the Roof.
                m.SendLocalizedMessage(1156196);
                return false;
            }
        }

        if (p != null)
        {
            foreach (var info in p.Members)
            {
                foreach (var enc in Encounters)
                {
                    if (enc.PartyLeader != null)
                    {
                        var party = Party.Get(enc.PartyLeader);

                        if (enc.PartyLeader == info.Mobile || party?.Contains(info.Mobile) == true)
                        {
                            // ~1_NAME~ in your party is already attempting to join a Shadowguard
                            // encounter. Start a new party without them or wait until they are
                            // finished and try again.
                            m.SendLocalizedMessage(1156189, info.Mobile.Name);
                            return false;
                        }
                    }

                    foreach (var queued in Queue.Keys.Where(l => l != null))
                    {
                        var party = Party.Get(queued);

                        if (queued == info.Mobile || party?.Contains(info.Mobile) == true)
                        {
                            m.SendLocalizedMessage(1156189, info.Mobile.Name);
                            return false;
                        }
                    }
                }
            }
        }

        return Encounters.All(instance => instance.PartyLeader != m);
    }

    public void StartTimer()
    {
        EndTimer();
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick);
    }

    public void EndTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    public static int GetLocalization(EncounterType encounter) =>
        encounter switch
        {
            EncounterType.Orchard  => 1156166,
            EncounterType.Armory   => 1156167,
            EncounterType.Fountain => 1156168,
            EncounterType.Belfry   => 1156169,
            EncounterType.Roof     => 1156170,
            _                      => 1156165 // Bar
        };

    public ShadowguardInstance GetAvailableInstance(EncounterType type)
    {
        if (RandomInstances)
        {
            var candidates = type == EncounterType.Roof
                ? Instances.Where(e => e.IsRoof && !e.InUse).ToList()
                : Instances.Where(e => !e.IsRoof && !e.InUse).ToList();

            return candidates.Count > 0 ? candidates[Utility.Random(candidates.Count)] : null;
        }

        return type == EncounterType.Roof
            ? Instances.FirstOrDefault(e => e.IsRoof && !e.InUse)
            : Instances.FirstOrDefault(e => !e.IsRoof && !e.InUse);
    }

    public void AddToQueue(Mobile m, EncounterType encounter)
    {
        if (Queue.ContainsKey(m))
        {
            m.SendLocalizedMessage(encounter == EncounterType.Roof ? 1156245 : 1156246);
            /* You are currently already in the queue for [the finale/one of the tower encounters].
            You cannot join this queue unless you leave the other queue. Use the context menu
            option on the crystal ball to exit that queue. */
            return;
        }

        Queue.Add(m, encounter);

        var order = Array.IndexOf(Queue.Keys.ToArray(), m) + 1;

        /* The fortress is currently full right now. You are currently ~1_NUM~ in the queue.
        You will be messaged when an encounter is available. You must remain in the lobby in
        order to be able to join. */
        m.SendLocalizedMessage(1156182, order > 1 ? order.ToString() : "next");
    }

    public bool IsInQueue(Mobile m) => Queue.ContainsKey(m);

    public bool RemoveFromQueue(Mobile m) => Queue.Remove(m);

    public void CheckQueue()
    {
        if (Queue.Count == 0)
        {
            return;
        }

        var message = false;
        var copy = new List<Mobile>(Queue.Keys);

        for (var i = 0; i < copy.Count; i++)
        {
            var m = copy[i];

            if (m.Map != Map.TerMur || m.NetState == null)
            {
                RemoveFromQueue(m);
                message |= i == 0;
                continue;
            }

            if (Encounters.Any(inst => inst.PartyLeader == m))
            {
                message |= i == 0;
                RemoveFromQueue(m);
                continue;
            }

            message = true;

            Timer.DelayCall(TimeSpan.FromMinutes(2), () =>
            {
                if (!Queue.TryGetValue(m, out var type))
                {
                    return;
                }

                var instance = GetAvailableInstance(type);

                if (instance != null && instance.TryBeginEncounter(m, true, type))
                {
                    RemoveFromQueue(m);
                }
            });

            break;
        }

        if (!message || Queue.Count <= 0)
        {
            return;
        }

        var index = 0;
        foreach (var mob in Queue.Keys)
        {
            var displayIndex = index + 1 > 1 ? index.ToString() : "next";
            var p = Party.Get(mob);

            if (p != null)
            {
                foreach (var info in p.Members)
                {
                    // A Shadowguard encounter has opened. You are currently ~1_NUM~ in the
                    // queue. If you are next, you may proceed to the entry stone to join.
                    info.Mobile.SendLocalizedMessage(1156190, displayIndex);
                }
            }
            else
            {
                mob.SendLocalizedMessage(1156190, displayIndex);
            }

            index++;
        }
    }

    public static readonly Rectangle2D[] EncounterBounds =
    {
        new(70, 1990, 51, 51), new(198, 1990, 51, 51), new(326, 1990, 51, 51), new(454, 1990, 51, 51),

        new(134, 2054, 51, 51), new(262, 2054, 51, 51), new(390, 2054, 51, 51),

        new(70, 2118, 51, 51), new(198, 2118, 51, 51), new(326, 2118, 51, 51),

        new(134, 2182, 51, 51), new(262, 2182, 51, 51), new(390, 2182, 51, 51),

        new(31, 2303, 64, 64), new(127, 2303, 64, 64), new(31, 2399, 64, 64), new(127, 2399, 64, 64)
    };

    public static readonly Point3D[] CenterPoints =
    {
        new(96, 2016, -20), new(224, 2016, -20), new(352, 2016, -20), new(480, 2016, -20),
        new(160, 2080, -20), new(288, 2080, -20), new(416, 2080, -20),
        new(96, 2144, -20), new(224, 2144, -20), new(352, 2144, -20),
        new(160, 2208, -20), new(288, 2208, -20), new(416, 2208, -20),

        new(64, 2336, 0), new(160, 2336, 0), new(64, 2432, 0), new(160, 2432, 0)
    };

    public static ShadowguardEncounter GetEncounter(Point3D p, Map map) =>
        Region.Find(p, map) is ShadowguardRegion r ? r.Instance.Encounter : null;

    public static ShadowguardInstance GetInstance(Point3D p, Map map) =>
        Region.Find(p, map) is ShadowguardRegion r ? r.Instance : null;

    public override void OnDelete()
    {
        base.OnDelete();

        EndTimer();

        if (Encounters != null)
        {
            foreach (var e in Encounters)
            {
                e.Reset();
            }

            Encounters = null;
        }

        if (Addons != null)
        {
            for (var i = Addons.Count - 1; i >= 0; i--)
            {
                Addons[i].Delete();
            }

            Addons = null;
        }

        if (Instances != null)
        {
            foreach (var inst in Instances)
            {
                if (inst.Region != null)
                {
                    inst.ClearRegion();
                    inst.Region.Unregister();
                }
            }

            Instances = null;
        }

        Queue.Clear();
        Table?.Clear();
        Table = null;

        Instance = null;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Instance = this;

        Encounters = new List<ShadowguardEncounter>();
        Queue = new Dictionary<Mobile, EncounterType>();
        Addons = new List<BaseAddon>();

        InitializeInstances();
        StartTimer();

        // ShadowguardPersistence (a separate GenericPersistence) populates Encounters/Table/Addons
        // from its own save file after this runs — see that class for why.
    }

    private static void OnDisconnected(Mobile m) => GetEncounter(m.Location, m.Map)?.CheckPlayerStatus(m);

    private static void OnConnected(Mobile m)
    {
        if (m.AccessLevel > AccessLevel.GameMaster)
        {
            return;
        }

        var inst = GetInstance(m.Location, m.Map);

        if (inst == null)
        {
            return;
        }

        var encounter = inst.Encounter;

        if (encounter == null)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1), () => ShadowguardEncounter.MovePlayer(m, Instance.KickLocation, true));
        }
        else if (m != encounter.PartyLeader && m is PlayerMobile pm && !encounter.Participants.Contains(pm))
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1), () => ShadowguardEncounter.MovePlayer(m, Instance.KickLocation, true));
        }
    }
}
