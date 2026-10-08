using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.BuffIcons;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/VvVBattle.cs).
// Simplifications: RunUO-era `ColUtility.Free`/`IPooledEnumerable` dropped/replaced with
// direct list use; `Region.GetEnumeratedMobiles()` -> `Region.GetMobiles()`;
// `GuardedRegion.Disabled`/`.Disable()` -> `GuardsDisabled`/`GuardedRegion.Disable()` (this
// codebase's real names); `Map.GetRandomSpawnPoint(Rectangle2D)` (doesn't exist here) replaced
// with a manual nearby-point search; `Aggression.CombatHeatDelay` (that class doesn't exist
// here) -> `HouseRegion.CombatHeatDelay`, the equivalent constant already used for the same
// "combat heat" travel-restriction concept elsewhere in this codebase.
public enum UpdateType
{
    Kill,
    Assist,
    Steal,
    TurnInVice,
    TurnInVirtue,
    Disarm
}

[PropertyObject]
public class VvVBattle
{
    public static readonly int Duration = 30;
    public static readonly int Cooldown = 5;
    public static readonly int Announcement = 2;
    public static readonly int KillCooldownDuration = 5;
    public static readonly int MaxTraps = 20;
    public static readonly int MaxTurrets = 10;

    public static readonly double ScoreToWin = 10000;
    public static readonly double OccupyPoints = 300;
    public static readonly double AltarPoints = 1000;
    public static readonly double KillPoints = 600;
    public static readonly double TurnInPoints = 500;

    public static readonly int AltarSilver = 100;
    public static readonly int TurnInSilver = 100;
    public static readonly int KillSilver = 50;
    public static readonly int WinSilver = 100;
    public static readonly int DisarmSilver = 50;

    // Silver penalty in the event the battle is uncontested.
    public static readonly double Penalty = 0.5;

    [CommandProperty(AccessLevel.GameMaster)]
    public double SilverPenalty => !ViceVsVirtueSystem.EnhancedRules ? 1.0 : UnContested ? Penalty : 1.0;

    [CommandProperty(AccessLevel.GameMaster)]
    public ViceVsVirtueSystem System { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime StartTime { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime CooldownEnds { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime LastOccupationCheck { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextSigilSpawn { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextAnnouncement { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextAltarActivate { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextManaSpike { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime ManaSpikeEndEffects { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public VvVCity City { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public VvVSigil Sigil { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool OnGoing { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool UnContested { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Region Region => VvVCityInfo.Infos[City]?.Region;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ForceStart
    {
        get => false;
        set
        {
            if (!OnGoing && value)
            {
                Begin();
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ForceEnd
    {
        get => false;
        set
        {
            if (OnGoing && value)
            {
                EndBattle();
            }
        }
    }

    public List<BattleTeam> Teams { get; set; }
    public Dictionary<Mobile, DateTime> KillCooldown { get; set; }
    public Dictionary<Mobile, DateTime> BattleAggression { get; set; }
    public List<string> Messages { get; set; }

    public List<VvVAltar> Altars { get; set; }
    public int AltarIndex { get; set; }

    public List<VvVTrap> Traps { get; set; }
    public List<CannonTurret> Turrets { get; set; }
    public List<Mobile> Warned { get; set; }

    public VvVPriest VicePriest { get; private set; }
    public VvVPriest VirtuePriest { get; private set; }

    public Timer Timer { get; private set; }

    public int TrapCount => Traps.Count(t => !t.Deleted);
    public int TurretCount => Turrets.Count(t => !t.Deleted);

    [CommandProperty(AccessLevel.GameMaster)]
    public bool InCooldown => CooldownEnds > Core.Now;

    public DateTime NextCombatHeatCycle { get; private set; }

    public VvVBattle(ViceVsVirtueSystem sys) => System = sys;

    public void Begin()
    {
        OnGoing = true;
        NextCombatHeatCycle = Core.Now;
        var newCity = City;
        var cities = new List<VvVCity>();

        Teams = new List<BattleTeam>();
        KillCooldown = new Dictionary<Mobile, DateTime>();
        Messages = new List<string>();
        Altars = new List<VvVAltar>();
        Traps = new List<VvVTrap>();
        Warned = new List<Mobile>();
        Turrets = new List<CannonTurret>();

        for (var i = 0; i < 8; i++)
        {
            if (!System.ExemptCities.Contains((VvVCity)i) && (VvVCity)i != newCity)
            {
                cities.Add((VvVCity)i);
            }
        }

        if (cities.Count > 0)
        {
            newCity = cities[Utility.Random(cities.Count)];
        }
        else if (System.ExemptCities.Contains(newCity))
        {
            System.SendVvVMessage("All VvV cities are currently exempt.");
            return;
        }

        City = newCity;
        BeginTimer();

        StartTime = Core.Now;
        NextSigilSpawn = Core.Now + TimeSpan.FromMinutes(Utility.RandomMinMax(1, 3));

        AltarIndex = 0;
        SpawnAltars();
        SpawnPriests(false);

        if (Region is GuardedRegion guarded)
        {
            GuardedRegion.Disable(guarded);
        }

        NextAltarActivate = Core.Now + TimeSpan.FromMinutes(1);

        // A Battle between Vice and Virtue is active! To Arms! The City of ~1_CITY~ is besieged!
        System.SendVvVMessage(1154721, $"#{ViceVsVirtueSystem.GetCityLocalization(City)}");
    }

    public void SpawnAltars()
    {
        foreach (var p in VvVCityInfo.Infos[City].AltarLocs)
        {
            var altar = new VvVAltar(this);
            altar.MoveToWorld(p, Map.Felucca);
            Altars.Add(altar);
        }
    }

    public void SpawnPriests(bool moveToWorld = true)
    {
        if (VicePriest is not { Deleted: false })
        {
            VicePriest = new VvVPriest(VvVType.Vice, this);
        }

        if (VirtuePriest is not { Deleted: false })
        {
            VirtuePriest = new VvVPriest(VvVType.Virtue, this);
        }

        if (!moveToWorld)
        {
            return;
        }

        VicePriest.MoveToWorld(FindNearbyPoint(VvVCityInfo.Infos[City].PriestLocation), Map.Felucca);
        VirtuePriest.MoveToWorld(FindNearbyPoint(VvVCityInfo.Infos[City].PriestLocation), Map.Felucca);
    }

    private static Point3D FindNearbyPoint(Rectangle2D bounds)
    {
        for (var i = 0; i < 20; i++)
        {
            var x = Utility.RandomMinMax(bounds.X, bounds.X + bounds.Width);
            var y = Utility.RandomMinMax(bounds.Y, bounds.Y + bounds.Height);
            var z = Map.Felucca.GetAverageZ(x, y);

            if (Map.Felucca.CanSpawnMobile(x, y, z))
            {
                return new Point3D(x, y, z);
            }
        }

        return new Point3D(bounds.X, bounds.Y, Map.Felucca.GetAverageZ(bounds.X, bounds.Y));
    }

    public void BeginTimer()
    {
        EndTimer();
        Timer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnTick);
    }

    public void EndTimer()
    {
        Timer?.Stop();
        Timer = null;
    }

    public void OnTick()
    {
        if (!OnGoing)
        {
            return;
        }

        CheckParticipation();
        UpdateAllGumps();

        if (StartTime + TimeSpan.FromMinutes(Duration) < Core.Now)
        {
            EndBattle();
            return;
        }

        if (LastOccupationCheck + TimeSpan.FromMinutes(1) < Core.Now)
        {
            CheckOccupation();
            LastOccupationCheck = Core.Now;
        }

        if (NextAltarActivate != DateTime.MinValue && NextAltarActivate < Core.Now)
        {
            ActivateAltar();
            NextAltarActivate = DateTime.MinValue;
        }

        if (NextSigilSpawn != DateTime.MinValue && NextSigilSpawn < Core.Now && Sigil is not { Deleted: false })
        {
            SpawnSigil();
        }

        ActivateArrows();

        if (KillCooldown != null)
        {
            foreach (var m in KillCooldown.Keys.Where(mob => KillCooldown[mob] < Core.Now).ToList())
            {
                KillCooldown.Remove(m);
            }
        }

        foreach (var t in Turrets)
        {
            t.Scan();
        }
    }

    public void CheckParticipation()
    {
        if (Region == null)
        {
            return;
        }

        BattleTeam team = null;
        UnContested = true;
        var checkAggression = ViceVsVirtueSystem.EnhancedRules && NextCombatHeatCycle < Core.Now;

        foreach (var pm in Region.GetMobiles().OfType<PlayerMobile>())
        {
            var vvv = ViceVsVirtueSystem.IsVvV(pm);

            if (!vvv && !Warned.Contains(pm) && pm.AccessLevel == AccessLevel.Player)
            {
                pm.SendGump(new BattleWarningGump(pm));
                Warned.Add(pm);
            }
            else if (vvv && pm.Alive && !pm.Hidden && BaseBoat.FindBoatAt(pm.Location, pm.Map) == null && BaseHouse.FindHouseAt(pm) == null)
            {
                if (pm.Guild is Guild g)
                {
                    var t = GetTeam(g);

                    if (team == null)
                    {
                        team = t;
                    }
                    else if (t != team && UnContested)
                    {
                        UnContested = false;
                    }
                }
            }

            if (checkAggression && (vvv || ViceVsVirtueSystem.IsVvVCombatant(pm)))
            {
                AddAggression(pm);
            }
        }

        if (checkAggression)
        {
            CheckBattleAggression();
            NextCombatHeatCycle = Core.Now + TimeSpan.FromMinutes(1);
        }
    }

    public void AddAggression(Mobile m)
    {
        if (m is not PlayerMobile pm)
        {
            return;
        }

        BattleAggression ??= new Dictionary<Mobile, DateTime>();

        pm.AddBuff(new BuffInfo(BuffIcon.HeatOfBattleStatus, 1153801, 1153827, HouseRegion.CombatHeatDelay));
        BattleAggression[m] = Core.Now + TimeSpan.FromMinutes(2);
    }

    private void CheckBattleAggression()
    {
        if (BattleAggression == null)
        {
            return;
        }

        foreach (var m in new List<Mobile>(BattleAggression.Keys))
        {
            if (BattleAggression[m] < Core.Now && !m.Region.IsPartOf(Region))
            {
                BattleAggression.Remove(m);
            }
        }
    }

    public bool HasBattleAggression(Mobile m)
    {
        if (BattleAggression == null || !BattleAggression.TryGetValue(m, out var expires))
        {
            return false;
        }

        if (expires < Core.Now)
        {
            BattleAggression.Remove(m);
            return false;
        }

        return true;
    }

    public void EndBattle()
    {
        OnGoing = false;
        EndTimer();

        if (Region is GuardedRegion guarded)
        {
            guarded.GuardsDisabled = false;

            foreach (var pm in Region.GetMobiles().OfType<PlayerMobile>())
            {
                pm.RecheckTownProtection();
            }
        }

        foreach (var altar in Altars)
        {
            if (!altar.Deleted)
            {
                altar.Delete();
            }
        }

        foreach (var trap in Traps)
        {
            if (!trap.Deleted)
            {
                trap.Delete();
            }
        }

        foreach (var turret in Turrets)
        {
            if (!turret.Deleted)
            {
                turret.Delete();
            }
        }

        VicePriest?.Delete();
        VicePriest = null;

        VirtuePriest?.Delete();
        VirtuePriest = null;

        Sigil?.Delete();
        Sigil = null;

        TallyStats();
        SendBattleStatsGump();

        System.SendVvVMessage(1154722); // A VvV battle has just concluded. The next battle will begin in less than five minutes!

        BattleAggression?.Clear();

        KillCooldown.Clear();

        NextSigilSpawn = DateTime.MinValue;
        LastOccupationCheck = DateTime.MinValue;
        NextAnnouncement = DateTime.MinValue;
        StartTime = DateTime.MinValue;
        NextAltarActivate = DateTime.MinValue;
        ManaSpikeEndEffects = DateTime.MinValue;
        NextManaSpike = DateTime.MinValue;

        CooldownEnds = Core.Now + TimeSpan.FromMinutes(Cooldown);

        Timer.DelayCall(TimeSpan.FromMinutes(Cooldown), () => System.CheckBattleStatus());
    }

    public void TallyStats()
    {
        var leader = GetLeader();
        var added = new List<Guild>();

        if (leader?.Guild == null)
        {
            return;
        }

        leader.Silver += AwardSilver(WinSilver + OppositionCount(leader.Guild) * 50);

        foreach (var m in Region.GetMobiles())
        {
            if (m.Guild is not Guild g || m is not PlayerMobile pm)
            {
                continue;
            }

            var team = GetTeam(g);
            var stats = GetPlayerStats(pm);
            var entry = ViceVsVirtueSystem.Instance.GetPlayerEntry(pm);

            if (entry == null)
            {
                continue;
            }

            entry.Score += team.Score;
            entry.Points += team.Silver;
            entry.Kills += stats.Kills;
            entry.Deaths += stats.Deaths;
            entry.Assists += stats.Assists;
            entry.ReturnedSigils += stats.ReturnedSigils;
            entry.DisarmedTraps += stats.Disarmed;
            entry.StolenSigils += stats.Stolen;

            if (added.Contains(g))
            {
                continue;
            }

            added.Add(g);

            if (!ViceVsVirtueSystem.Instance.GuildStats.ContainsKey(g))
            {
                ViceVsVirtueSystem.Instance.GuildStats[g] = new VvVGuildStats(g);
            }

            var gstats = ViceVsVirtueSystem.Instance.GuildStats[g];

            gstats.Kills += team.Kills;
            gstats.ReturnedSigils += team.ReturnedSigils;
            gstats.Score += team.Score;
        }
    }

    public void SpawnSigil()
    {
        var locs = VvVCityInfo.Infos[City].SigilLocs;
        var p = locs[Utility.Random(locs.Length)];

        Sigil = new VvVSigil(this, p);
        Sigil.MoveToWorld(p, Map.Felucca);

        UpdateAllGumps();
    }

    public void ActivateAltar()
    {
        AltarIndex = AltarIndex == 2 ? 0 : AltarIndex + 1;

        Altars[AltarIndex].Activate();
        ActivateArrows();

        SendStatusMessage("Fight for the altar!", true);
    }

    // No-ops: see VvVAltar.cs's AltarArrow comment for why (QuestArrow here can't target a
    // non-Mobile Item like VvVAltar).
    public void CheckArrow(PlayerMobile pm)
    {
    }

    public void ActivateArrows()
    {
    }

    public VvVPlayerBattleStats GetPlayerStats(PlayerMobile pm)
    {
        if (pm?.Guild is not Guild g)
        {
            return null;
        }

        var team = GetTeam(g);
        var stats = team.PlayerStats.FirstOrDefault(s => s.Player == pm);

        if (stats == null)
        {
            stats = new VvVPlayerBattleStats(pm);
            team.PlayerStats.Add(stats);
        }

        return stats;
    }

    public BattleTeam GetTeam(Guild g)
    {
        var team = Teams.FirstOrDefault(t => t.Guild != null && (t.Guild == g || t.Guild.IsAlly(g)));

        if (team != null)
        {
            return team;
        }

        team = new BattleTeam(g);
        Teams.Add(team);

        return team;
    }

    public void Update(Mobile m, UpdateType type)
    {
        var entry = System.GetPlayerEntry(m as PlayerMobile);

        if (entry != null)
        {
            Update(null, entry, type);
        }
    }

    public void Update(VvVPlayerEntry victim, VvVPlayerEntry killer, UpdateType type)
    {
        if (killer?.Player == null || killer.Guild == null)
        {
            return;
        }

        var killerStats = GetPlayerStats(killer.Player);
        var victimStats = victim == null ? null : GetPlayerStats(victim.Player);

        var killerTeam = GetTeam(killer.Guild);
        var victimTeam = victim != null ? GetTeam(victim.Guild) : null;

        switch (type)
        {
            case UpdateType.Kill:
                if (killerStats != null)
                {
                    killerStats.Kills++;
                }

                if (victimStats != null)
                {
                    victimStats.Deaths++;
                }

                if (killerTeam != null)
                {
                    killerTeam.Kills++;
                }

                if (victimTeam != null)
                {
                    victimTeam.Deaths++;
                }

                if (victim?.Player != null)
                {
                    if (!KillCooldown.TryGetValue(victim.Player, out var cooldown) || cooldown < Core.Now)
                    {
                        if (killerTeam != null)
                        {
                            killerTeam.Score += (int)KillPoints;
                            killerTeam.Silver += AwardSilver(KillSilver + OppositionCount(killer.Guild) * 50);
                        }

                        SendStatusMessage($"{killer.Player.Name} has killed {victim.Player.Name}!");
                        KillCooldown[victim.Player] = Core.Now + TimeSpan.FromMinutes(KillCooldownDuration);
                    }
                }

                break;
            case UpdateType.Assist:
                if (killerStats != null)
                {
                    killerStats.Assists++;
                }

                if (killerTeam != null)
                {
                    killerTeam.Assists++;
                }

                break;
            case UpdateType.Steal:
                if (killerStats != null)
                {
                    killerStats.Stolen++;
                    SendStatusMessage($"{killer.Player.Name} has stolen the sigil!");
                }

                if (killerTeam != null)
                {
                    killerTeam.Stolen++;
                }

                break;
            case UpdateType.TurnInVice:
            case UpdateType.TurnInVirtue:
                if (killerTeam != null)
                {
                    killerTeam.Score += (int)TurnInPoints;
                    killerTeam.Silver += AwardSilver(TurnInSilver + OppositionCount(killer.Guild) * 50);
                }

                if (killerStats != null && killerTeam != null)
                {
                    if (type == UpdateType.TurnInVirtue)
                    {
                        killerStats.VirtueReturned++;
                        killerTeam.VirtueReturned++;
                    }
                    else
                    {
                        killerStats.ViceReturned++;
                        killerTeam.ViceReturned++;
                    }
                }

                SendStatusMessage($"{killer.Player.Name} has returned the sigil!");

                NextSigilSpawn = Core.Now + TimeSpan.FromMinutes(1);
                RemovePriests();

                break;
            case UpdateType.Disarm:
                SendStatusMessage($"{killer.Player.Name} has disarmed a trap!");

                if (killerStats != null)
                {
                    killerStats.Disarmed++;
                }

                if (killerTeam != null)
                {
                    killerTeam.Silver += AwardSilver(DisarmSilver + OppositionCount(killer.Guild) * 50);
                    killerTeam.Disarmed++;
                }

                break;
        }

        CheckScore();
    }

    public int AwardSilver(int amount) => (int)(amount * SilverPenalty);

    public void RemovePriests() =>
        Timer.DelayCall(
            TimeSpan.FromSeconds(4),
            () =>
            {
                VicePriest?.Internalize();
                VirtuePriest?.Internalize();
            }
        );

    public void OccupyAltar(Guild g)
    {
        if (!OnGoing || g == null)
        {
            return;
        }

        var team = GetTeam(g);

        team.Score += (int)AltarPoints;
        team.Silver += AwardSilver(AltarSilver + OppositionCount(g) * 50);

        SendStatusMessage($"{g.Abbreviation ?? "somebody"} claimed the altar!");

        foreach (var p in Region.GetMobiles().OfType<PlayerMobile>())
        {
            p.QuestArrow = null;
        }

        CheckScore();
        NextAltarActivate = Core.Now + TimeSpan.FromMinutes(2);
    }

    public void CheckOccupation()
    {
        if (!OnGoing)
        {
            return;
        }

        if (Teams.Count == 1)
        {
            var team = Teams[0];

            team.Score += (int)OccupyPoints;
            UpdateAllGumps();
            CheckScore();

            if (OnGoing && NextAnnouncement < Core.Now)
            {
                if (ViceVsVirtueSystem.EnhancedRules)
                {
                    System.SendVvVMessage($"{team.Guild.Name} is occupying {(City == VvVCity.SkaraBrae ? "Skara Brae" : City.ToString())}!");
                }
                else
                {
                    System.SendVvVMessage(1154957, team.Guild.Name); // ~1_NAME~ is occupying the city!
                }

                NextAnnouncement = Core.Now + TimeSpan.FromMinutes(Announcement);
            }
        }
        else if (NextAnnouncement < Core.Now)
        {
            if (ViceVsVirtueSystem.EnhancedRules)
            {
                System.SendVvVMessage(
                    1050039,
                    $"#{ViceVsVirtueSystem.GetCityLocalization(City)}\tis unoccupied! Slay opposing forces to claim the city for your guild!"
                );
            }
            else
            {
                System.SendVvVMessage(1154958); // The City is unoccupied! Slay opposing forces to claim the city for your guild!
            }

            NextAnnouncement = Core.Now + TimeSpan.FromMinutes(Announcement);
        }
    }

    public void CheckScore()
    {
        GetLeader(out var score);

        if (score >= ScoreToWin)
        {
            EndBattle();
            return;
        }

        UpdateAllGumps();
    }

    public BattleTeam GetLeader() => GetLeader(out _);

    public BattleTeam GetLeader(out int score)
    {
        score = 0;

        var teams = new List<BattleTeam>(Teams);
        teams.Sort();

        if (teams.Count > 0)
        {
            score = teams[0].Score;
            return teams[0];
        }

        return null;
    }

    // Returns enemy count, by alliance.
    public int OppositionCount(Guild g)
    {
        var count = 0;

        foreach (var team in Teams)
        {
            if (team.Guild == null || team.Guild == g || team.Guild.IsAlly(g))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public bool IsInActiveBattle(Mobile one, Mobile two) => IsInActiveBattle(one) && IsInActiveBattle(two);

    public bool IsInActiveBattle(Mobile m)
    {
        if (!OnGoing)
        {
            return false;
        }

        return Region.Find(m.Location, m.Map) == Region;
    }

    public void OnEnterRegion(Mobile m)
    {
        if (m is PlayerMobile pm && OnGoing)
        {
            pm.GetGumps().Close<VvVBattleStatusGump>();
            pm.SendGump(new VvVBattleStatusGump(pm, this));
        }
    }

    public void CheckGump(Mobile m)
    {
        if (m is PlayerMobile pm && OnGoing)
        {
            pm.SendGump(new VvVBattleStatusGump(pm, this));
        }
    }

    public void UpdateAllGumps()
    {
        if (Region == null)
        {
            return;
        }

        foreach (var m in Region.GetMobiles().OfType<PlayerMobile>())
        {
            if (!ViceVsVirtueSystem.IsVvV(m) || m.NetState == null)
            {
                continue;
            }

            if (m.GetGumps().Find<VvVBattleStatusGump>() is { } g)
            {
                g.Refresh(true, false);
            }
            else
            {
                m.SendGump(new VvVBattleStatusGump(m, this));
            }
        }
    }

    public void SendBattleStatsGump()
    {
        if (Region == null)
        {
            return;
        }

        foreach (var m in Region.GetMobiles().OfType<PlayerMobile>())
        {
            if (ViceVsVirtueSystem.IsVvV(m))
            {
                m.GetGumps().Close<VvVBattleStatusGump>();
                m.SendGump(new BattleStatsGump(m, this));
            }
        }
    }

    public void SendStatusMessage(string message, bool sendGumps = false)
    {
        Messages.Add(message);

        if (sendGumps)
        {
            UpdateAllGumps();
        }
    }

    public void AddCannonTurret(CannonTurret turret)
    {
        if (!Turrets.Contains(turret))
        {
            Turrets.Add(turret);
        }
    }

    // Manual Serialize/Deserialize (not [SerializableField]-based codegen): VvVBattle is a
    // [PropertyObject], not an Item/Mobile entity, so it's persisted as part of
    // ViceVsVirtueSystem's own save via VvVPersistence (see that file) rather than through the
    // Item/Mobile serialization pipeline.
    public void Serialize(IGenericWriter writer)
    {
        writer.Write(0); // version
        writer.Write(OnGoing);

        if (!OnGoing)
        {
            return;
        }

        writer.Write(StartTime);
        writer.Write(CooldownEnds);
        writer.Write(LastOccupationCheck);
        writer.Write(NextSigilSpawn);
        writer.Write(NextAnnouncement);
        writer.Write(NextAltarActivate);
        writer.Write((int)City);
        writer.Write(Sigil);
        writer.Write(VicePriest);
        writer.Write(VirtuePriest);

        writer.Write(Altars.Count);
        Altars.ForEach(altar => writer.Write(altar));

        writer.Write(Teams.Count);
        foreach (var team in Teams)
        {
            writer.Write(team.Guild);
            writer.Write(team.Score);
            writer.Write(team.Silver);
            writer.Write(team.Kills);
            writer.Write(team.Assists);
            writer.Write(team.Deaths);
            writer.Write(team.Stolen);
            writer.Write(team.ViceReturned);
            writer.Write(team.VirtueReturned);
            writer.Write(team.Disarmed);

            writer.Write(team.PlayerStats.Count);
            foreach (var stats in team.PlayerStats)
            {
                writer.Write(stats.Player);
                writer.Write(stats.Points);
                writer.Write(stats.Kills);
                writer.Write(stats.Assists);
                writer.Write(stats.Deaths);
                writer.Write(stats.Stolen);
                writer.Write(stats.ViceReturned);
                writer.Write(stats.VirtueReturned);
                writer.Write(stats.Disarmed);
            }
        }

        writer.Write(Traps.Count);
        Traps.ForEach(t => writer.Write(t));
    }

    public void Deserialize(IGenericReader reader)
    {
        reader.ReadInt(); // version

        Altars = new List<VvVAltar>();
        KillCooldown = new Dictionary<Mobile, DateTime>();
        Messages = new List<string>();
        Traps = new List<VvVTrap>();
        Warned = new List<Mobile>();
        Turrets = new List<CannonTurret>();
        Teams = new List<BattleTeam>();

        OnGoing = reader.ReadBool();

        if (!OnGoing)
        {
            return;
        }

        StartTime = reader.ReadDateTime();
        CooldownEnds = reader.ReadDateTime();
        LastOccupationCheck = reader.ReadDateTime();
        NextSigilSpawn = reader.ReadDateTime();
        NextAnnouncement = reader.ReadDateTime();
        NextAltarActivate = reader.ReadDateTime();
        City = (VvVCity)reader.ReadInt();
        Sigil = reader.ReadEntity<Item>() as VvVSigil;
        VicePriest = reader.ReadEntity<Mobile>() as VvVPriest;
        VirtuePriest = reader.ReadEntity<Mobile>() as VvVPriest;

        if (Sigil != null)
        {
            Sigil.Battle = this;
        }

        if (VicePriest != null)
        {
            VicePriest.Battle = this;
        }

        if (VirtuePriest != null)
        {
            VirtuePriest.Battle = this;
        }

        var altarCount = reader.ReadInt();
        for (var i = 0; i < altarCount; i++)
        {
            if (reader.ReadEntity<Item>() is VvVAltar altar)
            {
                altar.Battle = this;
                Altars.Add(altar);
            }
        }

        var teamCount = reader.ReadInt();
        for (var i = 0; i < teamCount; i++)
        {
            var g = reader.ReadEntity<Guilds.BaseGuild>() as Guild;
            var team = new BattleTeam(g)
            {
                Score = reader.ReadInt(),
                Silver = reader.ReadInt(),
                Kills = reader.ReadInt(),
                Assists = reader.ReadInt(),
                Deaths = reader.ReadInt(),
                Stolen = reader.ReadInt(),
                ViceReturned = reader.ReadInt(),
                VirtueReturned = reader.ReadInt(),
                Disarmed = reader.ReadInt()
            };

            var statsCount = reader.ReadInt();
            for (var j = 0; j < statsCount; j++)
            {
                var pm = reader.ReadEntity<Mobile>() as PlayerMobile;
                var stats = new VvVPlayerBattleStats(pm)
                {
                    Points = reader.ReadDouble(),
                    Kills = reader.ReadInt(),
                    Assists = reader.ReadInt(),
                    Deaths = reader.ReadInt(),
                    Stolen = reader.ReadInt(),
                    ViceReturned = reader.ReadInt(),
                    VirtueReturned = reader.ReadInt(),
                    Disarmed = reader.ReadInt()
                };

                if (pm != null)
                {
                    team.PlayerStats.Add(stats);
                }
            }

            Teams.Add(team);
        }

        var trapCount = reader.ReadInt();
        for (var i = 0; i < trapCount; i++)
        {
            if (reader.ReadEntity<Item>() is VvVTrap t)
            {
                Traps.Add(t);
            }
        }

        Timer.DelayCall(
            TimeSpan.FromSeconds(10),
            () =>
            {
                if (Region is GuardedRegion guarded)
                {
                    GuardedRegion.Disable(guarded);
                }

                BeginTimer();
                ActivateArrows();
            }
        );
    }
}
