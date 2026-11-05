using System;
using System.Text.Json.Serialization;
using Server.Engines.Blackthorn;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO's Blackthorn Revamped dungeon (Region.cs) — the dungeon half. While the
///     invasion event is running, a player who idles deep in the dungeon is enveloped by a noxious cloud
///     and dropped at a random point of the map, poisoned, with the surrounding monsters aggroed onto them.
///
///     Adapted: ServUO gated the sweep on <c>PointsSystem.FellowshipData.Enabled</c>; there is no points
///     framework here, so the sweep is gated on <see cref="InProgress" />, which the invasion controller
///     (Engines/Blackthorn/Invasion/) sets while an invasion runs.
///
///     The OnDeath hook feeds every creature death inside the region to that facet's
///     <see cref="InvasionController" />, which is how a wave learns its Captains and minions are dead.
/// </summary>
public class BlackthornDungeonRegion : DungeonRegion
{
    private static readonly Point3D[] RandomLocations =
    {
        new(6459, 2781, 0),
        new(6451, 2781, 0),
        new(6443, 2781, 0),
        new(6409, 2792, 0),
        new(6356, 2781, 0),
        new(6272, 2702, 0),
        new(6272, 2656, 0),
        new(6456, 2623, 0)
    };

    /// <summary>Set by the invasion controller while the event is running.</summary>
    public static bool InProgress { get; set; }

    private TimerExecutionToken _sweepToken;

    [JsonConstructor] // Don't include parent, since it is special
    public BlackthornDungeonRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public BlackthornDungeonRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public BlackthornDungeonRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override void OnRegister()
    {
        base.OnRegister();

        _sweepToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), Sweep, out _sweepToken);
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        _sweepToken.Cancel();
    }

    private void Sweep()
    {
        if (!InProgress)
        {
            return;
        }

        foreach (var m in GetMobiles())
        {
            if (m is not PlayerMobile || m.AccessLevel != AccessLevel.Player)
            {
                continue;
            }

            if (m.Hidden)
            {
                m.RevealingAction();
            }

            if (m.Y > 2575 && Core.TickCount - m.LastMoveTime > 120_000)
            {
                MoveLocation(m);
            }
        }
    }

    public void MoveLocation(Mobile m)
    {
        var p = RandomLocations[Utility.Random(RandomLocations.Length)];

        m.MoveToWorld(p, Map);

        for (var x = m.X - 1; x <= m.X + 1; x++)
        {
            for (var y = m.Y - 1; y <= m.Y + 1; y++)
            {
                Effects.SendLocationEffect(
                    new Point3D(x, y, m.Z),
                    Map,
                    Utility.RandomList(14120, 4518, 14133),
                    16,
                    1,
                    1166,
                    0
                );
            }
        }

        Effects.PlaySound(m.Location, Map, 0x231);
        m.LocalOverheadMessage(MessageType.Regular, 0x22, 500855); // You are enveloped by a noxious gas cloud!
        m.ApplyPoison(m, Poison.Lethal);

        foreach (var mob in Map.GetMobilesInRange(m.Location, 12))
        {
            if (mob.Combatant != null || mob is not BaseCreature creature || creature.GetMaster() != null ||
                !mob.CanBeHarmful(m))
            {
                continue;
            }

            if (mob.InLOS(m))
            {
                Timer.DelayCall(() => mob.Combatant = m);
            }
            else
            {
                creature.AIObject?.MoveTo(m, 1);
            }
        }

        m.LastMoveTime = Core.TickCount;
    }

    public override bool CheckTravel(Mobile traveller, Point3D p, TravelCheckType type, out TextDefinition message)
    {
        message = null;

        return traveller.AccessLevel > AccessLevel.Player || type > TravelCheckType.Mark;
    }

    public override void OnDeath(Mobile m)
    {
        base.OnDeath(m);

        if (m is not BaseCreature creature)
        {
            return;
        }

        if (Map == Map.Trammel)
        {
            InvasionController.TramInstance?.OnDeath(creature);
        }
        else if (Map == Map.Felucca)
        {
            InvasionController.FelInstance?.OnDeath(creature);
        }
    }
}
