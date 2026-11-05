using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (BedrollSpawner.cs) — the invisible spawner that keeps
///     the Wrong cell block stocked with bedrolls and links a few of them to the tunnels that drop players
///     into the dungeon proper. It restocks every two hours and clears its own children when removed.
///
///     Adapted: the repeating two-hour Timer became a one-shot token re-armed after each pass, its next
///     run persisted as a DateTime so a restart does not reset the cycle; the LINQ selection became a
///     plain loop; the version-0 Console line is gone with the version-0 migration.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BedrollSpawner : Item
{
    public static readonly Point3D[] RoomDestinations =
    {
        new(5651, 555, 20), new(5658, 562, 20), new(5666, 558, 20),
        new(5793, 594, 10), new(5790, 587, 10), new(5789, 579, 10), new(5790, 571, 10), new(5793, 563, 10),
        new(5792, 555, 10), new(5863, 594, 15), new(5864, 585, 15), new(5864, 578, 15), new(5863, 570, 15),
        new(5865, 562, 15), new(5868, 554, 15)
    };

    public static readonly Point3D[] OutsideTunnels =
    {
        new(5670, 550, 22), new(5721, 550, 20), new(5670, 535, 0), new(5864, 532, 15), new(5782, 536, 10)
    };

    private static readonly (Point3D Location, Type Type)[] Entries =
    {
        (new Point3D(5653, 565, 20), typeof(WrongBedrollEast)),
        (new Point3D(5651, 564, 20), typeof(WrongBedrollEast)),
        (new Point3D(5653, 561, 20), typeof(WrongBedrollEast)),
        (new Point3D(5650, 560, 20), typeof(WrongBedrollEast)),
        (new Point3D(5651, 555, 20), typeof(WrongBedrollEast)),
        (new Point3D(5651, 557, 20), typeof(WrongBedrollEast)),
        (new Point3D(5657, 554, 20), typeof(WrongBedrollEast)),
        (new Point3D(5659, 555, 20), typeof(WrongBedrollEast)),
        (new Point3D(5661, 558, 20), typeof(WrongBedrollEast)),
        (new Point3D(5657, 559, 20), typeof(WrongBedrollEast)),
        (new Point3D(5657, 561, 20), typeof(WrongBedrollEast)),
        (new Point3D(5660, 561, 20), typeof(WrongBedrollEast)),
        (new Point3D(5658, 563, 20), typeof(WrongBedrollEast)),
        (new Point3D(5657, 565, 20), typeof(WrongBedrollEast)),
        (new Point3D(5661, 563, 20), typeof(WrongBedrollEast)),
        (new Point3D(5666, 554, 20), typeof(WrongBedrollEast)),
        (new Point3D(5665, 557, 20), typeof(WrongBedrollEast)),
        (new Point3D(5668, 558, 20), typeof(WrongBedrollEast)),
        (new Point3D(5665, 560, 20), typeof(WrongBedrollSouth)),
        (new Point3D(5668, 563, 20), typeof(WrongBedrollEast)),
        (new Point3D(5665, 565, 20), typeof(WrongBedrollEast)),
        (new Point3D(5787, 594, 10), typeof(WrongBedrollEast)),
        (new Point3D(5789, 596, 10), typeof(WrongBedrollEast)),
        (new Point3D(5792, 596, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5792, 593, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5795, 596, 10), typeof(WrongBedrollEast)),
        (new Point3D(5795, 593, 10), typeof(WrongBedrollEast)),
        (new Point3D(5787, 588, 10), typeof(WrongBedrollEast)),
        (new Point3D(5790, 589, 10), typeof(WrongBedrollEast)),
        (new Point3D(5791, 585, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5793, 589, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5795, 585, 10), typeof(WrongBedrollEast)),
        (new Point3D(5787, 581, 10), typeof(WrongBedrollEast)),
        (new Point3D(5791, 577, 10), typeof(WrongBedrollEast)),
        (new Point3D(5793, 581, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5795, 579, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5787, 570, 10), typeof(WrongBedrollEast)),
        (new Point3D(5789, 573, 10), typeof(WrongBedrollEast)),
        (new Point3D(5792, 573, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5792, 570, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5795, 570, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5787, 561, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5788, 564, 10), typeof(WrongBedrollEast)),
        (new Point3D(5790, 562, 10), typeof(WrongBedrollEast)),
        (new Point3D(5791, 565, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5795, 565, 10), typeof(WrongBedrollEast)),
        (new Point3D(5795, 561, 10), typeof(WrongBedrollEast)),
        (new Point3D(5789, 553, 10), typeof(WrongBedrollEast)),
        (new Point3D(5790, 555, 10), typeof(WrongBedrollEast)),
        (new Point3D(5791, 557, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5794, 557, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5795, 554, 10), typeof(WrongBedrollSouth)),
        (new Point3D(5861, 596, 15), typeof(WrongBedrollEast)),
        (new Point3D(5862, 593, 15), typeof(WrongBedrollEast)),
        (new Point3D(5865, 596, 15), typeof(WrongBedrollEast)),
        (new Point3D(5866, 594, 15), typeof(WrongBedrollEast)),
        (new Point3D(5867, 592, 15), typeof(WrongBedrollEast)),
        (new Point3D(5860, 585, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5861, 588, 15), typeof(WrongBedrollEast)),
        (new Point3D(5864, 588, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5866, 585, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5867, 588, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5860, 577, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5861, 580, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5863, 580, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5864, 576, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5866, 579, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5867, 576, 15), typeof(WrongBedrollEast)),
        (new Point3D(5860, 568, 15), typeof(WrongBedrollEast)),
        (new Point3D(5861, 572, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5863, 569, 15), typeof(WrongBedrollEast)),
        (new Point3D(5865, 571, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5867, 568, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5860, 562, 15), typeof(WrongBedrollEast)),
        (new Point3D(5861, 560, 15), typeof(WrongBedrollEast)),
        (new Point3D(5862, 564, 15), typeof(WrongBedrollSouth)),
        (new Point3D(5864, 563, 15), typeof(WrongBedrollEast)),
        (new Point3D(5864, 561, 15), typeof(WrongBedrollEast)),
        (new Point3D(5866, 564, 15), typeof(WrongBedrollEast)),
        (new Point3D(5867, 561, 15), typeof(WrongBedrollEast)),
    };

    public readonly TimeSpan RestartDelay = TimeSpan.FromHours(2.0);

    [SerializableField(0)]
    private DateTime _nextRestart;

    [SerializableField(1)]
    private List<WrongBedrollBase> _bedrolls;

    [SerializableField(2)]
    private List<MysteriousTunnel> _mysteriousTunnels;

    private TimerExecutionToken _restartToken;

    public static List<BedrollSpawner> Instances { get; } = new();

    [Constructible]
    public BedrollSpawner() : base(3796)
    {
        Movable = false;
        Visible = false;

        _bedrolls = new List<WrongBedrollBase>();
        _mysteriousTunnels = new List<MysteriousTunnel>();

        Timer.DelayCall(TimeSpan.FromSeconds(10), CheckRespawn);

        _nextRestart = Core.Now + RestartDelay;
        ScheduleRestart();

        Instances.Add(this);
    }

    public override string DefaultName => $"Wrong Bedrolls Spawner {Map}";

    private void ScheduleRestart()
    {
        _restartToken.Cancel();

        var delay = _nextRestart - Core.Now;

        if (delay <= TimeSpan.Zero)
        {
            delay = TimeSpan.FromSeconds(1);
        }

        Timer.StartTimer(delay, OnRestart, out _restartToken);
    }

    private void OnRestart()
    {
        CheckRespawn();

        _nextRestart = Core.Now + RestartDelay;
        ScheduleRestart();
    }

    private void CheckRespawn()
    {
        if (Map == null || Map == Map.Internal)
        {
            return;
        }

        Cleanup();

        foreach (var (location, type) in Entries)
        {
            if (Activator.CreateInstance(type) is not WrongBedrollBase item)
            {
                continue;
            }

            item.Movable = false;
            item.MoveToWorld(location, Map);
            Bedrolls.Add(item);
        }

        for (var i = 0; i < OutsideTunnels.Length; i++)
        {
            var tunnel = new MysteriousTunnel();

            if (i < 3)
            {
                var index = Utility.Random(Entries.Length);
                tunnel.PointDest = Bedrolls[index].Location;
                Bedrolls[index].PointDest = OutsideTunnels[i];
                Bedrolls[index].BedrollSpawner = this;
            }
            else
            {
                tunnel.PointDest = RoomDestinations[Utility.Random(RoomDestinations.Length)];

                foreach (var bedroll in Bedrolls)
                {
                    if (bedroll.PointDest == Point3D.Zero && bedroll.InRange(tunnel.PointDest, 4))
                    {
                        bedroll.PointDest = OutsideTunnels[i];
                        bedroll.BedrollSpawner = this;
                        break;
                    }
                }
            }

            tunnel.MoveToWorld(OutsideTunnels[i], Map);
            MysteriousTunnels.Add(tunnel);
        }
    }

    public void Cleanup()
    {
        foreach (var bedroll in Bedrolls)
        {
            bedroll.Delete();
        }

        Bedrolls.Clear();

        foreach (var tunnel in MysteriousTunnels)
        {
            tunnel.Delete();
        }

        MysteriousTunnels.Clear();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _restartToken.Cancel();

        Instances.Remove(this);
        Cleanup();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _bedrolls ??= new List<WrongBedrollBase>();
        _mysteriousTunnels ??= new List<MysteriousTunnel>();

        Instances.Add(this);

        if (_nextRestart < Core.Now)
        {
            _nextRestart = Core.Now;
        }

        ScheduleRestart();
    }
}