using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.CodeGeneratedEvents;
using Server.Commands;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/ViceVsVirtueSystem.cs).
// The biggest structural change: ServUO's `PointsSystem`/`PointsEntry` generic loyalty-points
// framework (used across many unrelated ServUO systems, not just VvV) doesn't exist in this
// codebase at all, so `ViceVsVirtueSystem` is a plain singleton here instead of a
// `PointsSystem` subclass, and `VvVPlayerEntry` is a plain class instead of a `PointsEntry`
// subclass — state lives directly in a `Dictionary<PlayerMobile, VvVPlayerEntry>` and is saved
// through `VvVPersistence : GenericPersistence` (the established pattern in this codebase,
// e.g. `Engines/Shadowguard/ShadowguardPersistence.cs`) instead of the points-system's own
// save format. `Config.Get` (RunUO config helper) doesn't exist — replaced with
// `ServerConfiguration.GetSetting`. RunUO-era `ColUtility.Free`/`IPooledEnumerable` dropped/
// replaced with direct enumeration. `RunicReforging`/`NegativeAttributes`-based item-antiquing
// (`FixVvVItems`, a migration for old saves) dropped — not needed for a brand-new system.
// `PVPArenaSystem.IsSameIP` (that engine doesn't exist here) dropped from `RestrictSilver` —
// simplified to an account-only check.
public enum VvVType
{
    Virtue,
    Vice
}

public enum VvVCity
{
    Britain,
    Jhelom,
    Minoc,
    Moonglow,
    Ocllo,
    SkaraBrae,
    Trinsic,
    Yew
}

public class ViceVsVirtueSystem
{
    public static int VirtueHue = 2124;
    public static int ViceHue = 2118;

    public static readonly Map Facet = Map.Felucca;

    public static bool Enabled = ServerConfiguration.GetSetting("vvv.enabled", true);
    public static int StartSilver = ServerConfiguration.GetSetting("vvv.startSilver", 2000);
    public static bool EnhancedRules = ServerConfiguration.GetSetting("vvv.enhancedRules", false);

    public static ViceVsVirtueSystem Instance { get; set; }

    public bool HasGenerated { get; set; }

    public Dictionary<PlayerMobile, VvVPlayerEntry> PlayerEntries { get; set; }
    public Dictionary<Guild, VvVGuildStats> GuildStats { get; set; }
    public static List<TemporaryCombatant> TempCombatants { get; set; }

    public List<VvVCity> ExemptCities { get; set; }

    public VvVBattle Battle { get; private set; }

    private static VvVPersistence _persistence;

    public ViceVsVirtueSystem()
    {
        Instance = this;
        Battle = new VvVBattle(this);

        PlayerEntries = new Dictionary<PlayerMobile, VvVPlayerEntry>();
        GuildStats = new Dictionary<Guild, VvVGuildStats>();
        ExemptCities = new List<VvVCity>();
    }

    public VvVPlayerEntry GetPlayerEntry(PlayerMobile pm, bool addIfMissing = false)
    {
        if (pm == null)
        {
            return null;
        }

        if (PlayerEntries.TryGetValue(pm, out var entry))
        {
            return entry;
        }

        if (!addIfMissing)
        {
            return null;
        }

        entry = new VvVPlayerEntry(pm);
        PlayerEntries[pm] = entry;
        return entry;
    }

    public void HandlePlayerDeath(PlayerMobile victim)
    {
        var ventry = GetPlayerEntry(victim);

        if (ventry is not { Active: true })
        {
            return;
        }

        var list = victim.DamageEntries.ToArray().OrderByDescending(d => d.DamageGiven).ToList();
        var handled = new List<Mobile>();
        var statloss = false;

        foreach (var damageEntry in list)
        {
            var dam = damageEntry.Damager;

            if (dam == victim || dam == null)
            {
                continue;
            }

            if (dam is BaseCreature { } bc && bc.GetMaster() is PlayerMobile master)
            {
                dam = master;
            }

            var isEnemy = IsEnemy(victim, dam);

            if (isEnemy)
            {
                var kentry = GetPlayerEntry(dam as PlayerMobile);

                if (kentry is { Active: true } && !handled.Contains(dam))
                {
                    if (Battle.IsInActiveBattle(dam, victim))
                    {
                        Battle.Update(ventry, kentry, list.IndexOf(damageEntry) == 0 ? UpdateType.Kill : UpdateType.Assist);
                    }

                    handled.Add(dam);
                    kentry.TotalKills++;

                    if (EnhancedRules)
                    {
                        kentry.AwardSilver(victim);
                    }
                }

                if (!handled.Contains(victim))
                {
                    ventry.TotalDeaths++;
                    handled.Add(victim);
                }
            }

            if (!statloss && isEnemy)
            {
                statloss = true;
            }
        }

        if (statloss)
        {
            ApplySkillLoss(victim);
        }
    }

    public void AddPlayer(PlayerMobile pm)
    {
        if (pm?.Guild == null)
        {
            return;
        }

        var g = (Guild)pm.Guild;
        var entry = GetPlayerEntry(pm, true);

        if (!entry.Active)
        {
            entry.Active = true;
        }

        pm.SendLocalizedMessage(1155564); // You have joined Vice vs Virtue!
        pm.SendLocalizedMessage(1063156, g.Name); // The guild information for ~1_val~ has been updated.

        pm.Delta(MobileDelta.Noto);
        pm.ProcessDelta();

        CheckBattleStatus(pm);
    }

    public bool IsResigning(PlayerMobile pm, Guild g)
    {
        var entry = GetPlayerEntry(pm);
        return entry != null && entry.Guild == g && entry.Resigning;
    }

    public void OnResign(Mobile m, bool quitguild = false)
    {
        var entry = GetPlayerEntry(m as PlayerMobile);

        if (entry is { Active: true, Resigning: false })
        {
            entry.ResignExpiration = m.AccessLevel == AccessLevel.Player
                ? Core.Now + TimeSpan.FromDays(3)
                : Core.Now + TimeSpan.FromMinutes(1);

            if (quitguild)
            {
                // You have quit a guild while participating in Vice vs Virtue. You will be
                // freely attackable by members of Vice vs Virtue until your resignation period has ended!
                m.SendLocalizedMessage(1155580);
            }
        }
    }

    public void CheckResignation(PlayerMobile pm)
    {
        var entry = GetPlayerEntry(pm);

        if (entry is { Resigning: true } && entry.ResignExpiration < Core.Now)
        {
            pm.PrivateOverheadMessage(MessageType.Regular, 1154, 1155561, pm.NetState); // You are no longer in Vice vs Virtue!

            entry.Active = false;
            entry.ResignExpiration = DateTime.MinValue;
            pm.Delta(MobileDelta.Noto);
            pm.ValidateEquipment();
        }
    }

    public void CheckBattleStatus()
    {
        if (Battle.OnGoing)
        {
            return;
        }

        if (EnemyGuildCount() > 0)
        {
            Battle.Begin();
        }
    }

    public void CheckBattleStatus(PlayerMobile pm)
    {
        if (!IsVvV(pm) || !Enabled)
        {
            return;
        }

        if (Battle.OnGoing)
        {
            SendVvVMessageTo(pm, 1154721, $"#{GetCityLocalization(Battle.City)}");
            // A Battle between Vice and Virtue is active! To Arms! The City of ~1_CITY~ is besieged!

            if (Battle.IsInActiveBattle(pm))
            {
                Battle.CheckGump(pm);
                Battle.CheckArrow(pm);
            }
        }
        else if (EnemyGuildCount() < 1)
        {
            SendVvVMessageTo(pm, 1154936); // More players are needed before a VvV battle can begin!
        }
        else if (Battle.InCooldown)
        {
            SendVvVMessageTo(pm, 1154722); // A VvV battle has just concluded. The next battle will begin in less than five minutes!
        }
        else
        {
            Battle.Begin();
        }
    }

    public int EnemyGuildCount()
    {
        var guilds = new List<Guild>();

        foreach (var ns in NetState.Instances)
        {
            var m = ns.Mobile;

            if (m?.Guild is not Guild g)
            {
                continue;
            }

            var entry = GetPlayerEntry(m as PlayerMobile);

            if (entry == null || entry.Guild != g || guilds.Contains(g))
            {
                continue;
            }

            if (!guilds.Any(guild => guild.IsAlly(g)))
            {
                guilds.Add(g);
            }
        }

        return guilds.Count - 1;
    }

    #region Skill Loss
    public const double SkillLossFactor = 1.0 / 3;
    public static TimeSpan SkillLossPeriod => TimeSpan.FromMinutes(5);

    private static readonly Dictionary<Mobile, SkillLossContext> _skillLoss = new();

    private class SkillLossContext
    {
        public Timer Timer;
        public List<SkillMod> Mods;
    }

    public static bool InSkillLoss(Mobile mob) => _skillLoss.ContainsKey(mob);

    public static void ApplySkillLoss(Mobile mob)
    {
        if (InSkillLoss(mob))
        {
            return;
        }

        var context = new SkillLossContext();
        _skillLoss[mob] = context;

        var mods = context.Mods = new List<SkillMod>();

        for (var i = 0; i < mob.Skills.Length; ++i)
        {
            var sk = mob.Skills[i];
            var baseValue = sk.Base;

            if (baseValue > 0)
            {
                var mod = new DefaultSkillMod(sk.SkillName, "VvVSkillLoss", true, -(baseValue * SkillLossFactor));
                mods.Add(mod);
                mob.AddSkillMod(mod);
            }
        }

        context.Timer = Timer.DelayCall(SkillLossPeriod, () => ClearSkillLoss(mob));
    }

    public static bool ClearSkillLoss(Mobile mob)
    {
        if (!_skillLoss.TryGetValue(mob, out var context))
        {
            return false;
        }

        _skillLoss.Remove(mob);

        foreach (var mod in context.Mods)
        {
            mob.RemoveSkillMod(mod);
        }

        context.Timer.Stop();

        return true;
    }
    #endregion

    public void SendVvVMessage(string message)
    {
        foreach (var state in NetState.Instances.Where(st => st.Mobile != null && IsVvV(st.Mobile)))
        {
            state.Mobile?.SendMessage($"[Guild][VvV] {message}");
        }
    }

    public void SendVvVMessage(int cliloc, string args = "")
    {
        foreach (var state in NetState.Instances.Where(st => st.Mobile != null && IsVvV(st.Mobile)))
        {
            if (state.Mobile != null)
            {
                SendVvVMessageTo(state.Mobile, cliloc, args);
            }
        }
    }

    public void SendVvVMessageTo(Mobile m, int cliloc, string args = "") =>
        m.SendLocalizedMessage(cliloc, false, "[Guild][VvV] ", args, m is PlayerMobile pm ? pm.GuildMessageHue : 0x34);

    private readonly List<Item> _vvvItems = new();

    public void AddVvVItem(Item item, bool initial = false)
    {
        if (!Enabled || item is not IVvVItem vvvItem)
        {
            return;
        }

        vvvItem.IsVvVItem = true;

        if (!_vvvItems.Contains(item))
        {
            _vvvItems.Add(item);
        }

        if (initial)
        {
            VvVEquipment.CheckProperties(item);
        }
    }

    public static void Configure()
    {
        VvVCityInfo.Configure();
        VvVRewards.Configure();
        VvVMount.Configure();
        Instance = new ViceVsVirtueSystem();
        _persistence = new VvVPersistence();
    }

    public static void Initialize()
    {
        if (!Enabled)
        {
            return;
        }

        EventSink.Connected += OnLogin;

        VvVMount.Initialize();

        // No in-game entry point for joining VvV was found anywhere in ServUO's own
        // ViceVsVirtue folder (58 files) — it's presumably wired up from a guildstone or
        // similar file outside that folder, which wasn't part of this port. Added a plain
        // command as the join/leaderboard entry point instead.
        CommandSystem.Register(
            "VvV",
            AccessLevel.Player,
            e =>
            {
                if (e.Mobile is not PlayerMobile pm)
                {
                    return;
                }

                if (IsVvV(pm))
                {
                    pm.SendGump(new ViceVsVirtueLeaderboardGump(pm));
                }
                else
                {
                    pm.SendGump(new ConfirmSignupGump(pm));
                }
            }
        );

        CommandSystem.Register(
            "VvVBattleProps",
            AccessLevel.GameMaster,
            e =>
            {
                if (Instance.Battle != null)
                {
                    e.Mobile.SendGump(new PropertiesGump(e.Mobile, Instance.Battle));
                }
            }
        );

        CommandSystem.Register(
            "VvVForceStartBattle",
            AccessLevel.GameMaster,
            e =>
            {
                if (Instance.Battle is { OnGoing: false })
                {
                    Instance.Battle.Begin();
                }
            }
        );

        CommandSystem.Register(
            "VvVExemptCities",
            AccessLevel.Administrator,
            e => e.Mobile.SendGump(new ExemptCitiesGump())
        );

        CommandSystem.Register(
            "VvVKick",
            AccessLevel.GameMaster,
            e =>
            {
                e.Mobile.SendMessage("Укажи, кого убрать из ВвВ.");
                e.Mobile.BeginTarget(
                    -1,
                    false,
                    Targeting.TargetFlags.None,
                    (from, targeted) =>
                    {
                        if (targeted is not PlayerMobile pm)
                        {
                            return;
                        }

                        var entry = Instance.GetPlayerEntry(pm);

                        if (entry is { Active: true })
                        {
                            pm.PrivateOverheadMessage(MessageType.Regular, 1154, 1155561, pm.NetState); // You are no longer in Vice vs Virtue!

                            entry.Active = false;
                            entry.ResignExpiration = DateTime.MinValue;
                            pm.Delta(MobileDelta.Noto);
                            pm.ValidateEquipment();

                            from.SendMessage($"{pm.Name} has been removed from VvV.");
                            pm.SendMessage("Тебя убрали из ВвВ.");
                        }
                        else
                        {
                            from.SendMessage($"{pm.Name} is not an active VvV member.");
                        }
                    }
                );
            }
        );

        if (!Instance.HasGenerated)
        {
            CreateSilverTraders();
            Instance.HasGenerated = true;
        }
    }

    public static void OnLogin(Mobile m)
    {
        if (!Enabled || m is not PlayerMobile pm)
        {
            return;
        }

        Timer.DelayCall(TimeSpan.FromSeconds(1), () => Instance.CheckResignation(pm));
        Timer.DelayCall(TimeSpan.FromSeconds(2), () => Instance.CheckBattleStatus(pm));
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void OnPlayerDeath(PlayerMobile pm)
    {
        if (Enabled)
        {
            Instance.HandlePlayerDeath(pm);
        }
    }

    public static bool IsVvV(Mobile m, bool checkpet = true, bool guildedonly = false)
    {
        if (!Enabled)
        {
            return false;
        }

        if (m is BaseCreature bc && checkpet && bc.GetMaster() is PlayerMobile master)
        {
            m = master;
        }

        var entry = Instance.GetPlayerEntry(m as PlayerMobile);

        return entry != null && entry.Active && (!guildedonly || entry.Guild != null);
    }

    public static bool IsVvV(Mobile m, out VvVPlayerEntry entry, bool checkpet = true, bool guildedonly = false)
    {
        if (!Enabled)
        {
            entry = null;
            return false;
        }

        if (m is BaseCreature bc && checkpet && bc.GetMaster() is PlayerMobile master)
        {
            m = master;
        }

        entry = Instance.GetPlayerEntry(m as PlayerMobile);

        if (entry is { Active: false })
        {
            entry = null;
        }

        return entry != null && entry.Active && (!guildedonly || entry.Guild != null);
    }

    public static bool IsEnemy(Mobile from, Mobile to)
    {
        if (!Enabled || from == to)
        {
            return false;
        }

        if (from is BaseCreature { } fromBc && fromBc.GetMaster() is PlayerMobile fromMaster)
        {
            from = fromMaster;
        }

        if (to is BaseCreature { } toBc && toBc.GetMaster() is PlayerMobile toMaster)
        {
            to = toMaster;
        }

        if (!IsVvVCombatant(to) || !IsVvVCombatant(from))
        {
            return false;
        }

        return !IsAllied(from, to);
    }

    public static bool IsAllied(Mobile a, Mobile b)
    {
        if (a.Guild is Guild guildA && b.Guild is Guild guildB && (guildA == guildB || guildA.IsAlly(guildB)))
        {
            return true;
        }

        if (TempCombatants == null)
        {
            return false;
        }

        var tempA = TempCombatants.FirstOrDefault(c => c.From == a);
        var tempB = TempCombatants.FirstOrDefault(c => c.From == b);

        if (tempA != null && (tempA.Friendly == b || (tempA.FriendlyGuild != null && tempA.FriendlyGuild == b.Guild as Guild)))
        {
            return true;
        }

        if (tempB != null && (tempB.Friendly == a || (tempB.FriendlyGuild != null && tempB.FriendlyGuild == a.Guild as Guild)))
        {
            return true;
        }

        return false;
    }

    public static bool IsVvVCombatant(Mobile mobile)
    {
        CheckTempCombatants();
        return IsVvV(mobile) || (TempCombatants != null && TempCombatants.Any(c => c.From == mobile));
    }

    public static void CheckHarmful(Mobile attacker, Mobile defender)
    {
        CheckTempCombatants();

        if (attacker == null || defender == null || IsAllied(attacker, defender))
        {
            return;
        }

        if (!IsVvV(attacker) && IsVvV(defender) && !defender.Aggressed.Any(info => info.Defender == attacker))
        {
            AddTempParticipant(attacker, null);
        }
    }

    public static void CheckBeneficial(Mobile from, Mobile target)
    {
        CheckTempCombatants();

        if (from == null || target == null || (IsVvV(from) && IsAllied(from, target)))
        {
            return;
        }

        if (!IsVvV(from) && IsVvV(target))
        {
            if (target.Aggressors.Any(info => IsVvV(info.Attacker)) || target.Aggressed.Any(info => IsVvV(info.Defender)))
            {
                AddTempParticipant(from, target);
            }
        }
    }

    public static Timer TempCombatantTimer { get; private set; }

    public static void AddTempCombatantTimer() =>
        TempCombatantTimer ??= Timer.DelayCall(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), CheckTempCombatants);

    public static void StopTempCombatantTimer()
    {
        TempCombatantTimer?.Stop();
        TempCombatantTimer = null;
    }

    public static void CheckTempCombatants()
    {
        if (TempCombatants == null)
        {
            StopTempCombatantTimer();
            return;
        }

        for (var i = TempCombatants.Count - 1; i >= 0; i--)
        {
            if (TempCombatants[i].Expired)
            {
                TempCombatants.RemoveAt(i);
            }
        }
    }

    public static TemporaryCombatant GetTempCombatant(Mobile from, Mobile to)
    {
        foreach (var combatant in TempCombatants.Where(c => c.From == from))
        {
            if (combatant.Friendly == null && to == null)
            {
                return combatant;
            }

            if (combatant.Friendly == to || (combatant.FriendlyGuild != null && combatant.FriendlyGuild == from.Guild as Guild))
            {
                return combatant;
            }
        }

        return null;
    }

    public static void AddTempParticipant(Mobile m, Mobile friendlyTo)
    {
        if (TempCombatants == null)
        {
            TempCombatants = new List<TemporaryCombatant>();
            AddTempCombatantTimer();
        }

        var combatant = GetTempCombatant(m, friendlyTo);

        if (combatant == null)
        {
            combatant = new TemporaryCombatant(m, friendlyTo);
        }
        else
        {
            combatant.Reset();
        }

        TempCombatants.Add(combatant);

        m.Delta(MobileDelta.Noto);
        m.ProcessDelta();
    }

    public static void OnMapChange(PlayerMobile pm)
    {
        if (TempCombatants == null || pm.Map == Map.Internal || pm.Map == null)
        {
            return;
        }

        foreach (var temp in TempCombatants.Where(t => t.From == pm).ToList())
        {
            RemoveTempCombatant(temp);
        }
    }

    public static void RemoveTempCombatant(TemporaryCombatant tempCombatant)
    {
        if (TempCombatants == null)
        {
            return;
        }

        TempCombatants.Remove(tempCombatant);
        tempCombatant.From.Delta(MobileDelta.Noto);
        tempCombatant.From.ProcessDelta();

        if (TempCombatants.Count == 0)
        {
            TempCombatants = null;
            StopTempCombatantTimer();
        }
    }

    public static bool HasBattleAggression(Mobile m)
    {
        if (!EnhancedRules || Instance?.Battle is not { OnGoing: true })
        {
            return false;
        }

        return Instance.Battle.HasBattleAggression(m);
    }

    public static bool IsBattleRegion(Region r)
    {
        if (r == null || Instance == null)
        {
            return false;
        }

        return Instance.Battle.OnGoing && r.IsPartOf(Instance.Battle.Region);
    }

    public static int GetCityLocalization(VvVCity city) =>
        city switch
        {
            VvVCity.Moonglow  => 1011344,
            VvVCity.Britain   => 1011028,
            VvVCity.Jhelom    => 1011343,
            VvVCity.Yew       => 1011032,
            VvVCity.Minoc     => 1011031,
            VvVCity.Trinsic   => 1011029,
            VvVCity.SkaraBrae => 1011347,
            VvVCity.Ocllo     => 1076027,
            _                 => 0
        };

    public static void CreateSilverTraders()
    {
        if (!Enabled)
        {
            return;
        }

        var map = Map.Felucca;

        foreach (var info in VvVCityInfo.Infos.Values)
        {
            var found = false;

            foreach (var m in map.GetMobilesInRange(info.TraderLoc, 3))
            {
                if (m is SilverTrader)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var trader = new SilverTrader();
                trader.MoveToWorld(info.TraderLoc, map);
            }
        }
    }

    public static void DeleteSilverTraders()
    {
        foreach (var mob in World.Mobiles.Values.Where(m => m is SilverTrader).ToList())
        {
            mob.Delete();
        }
    }

    public static bool RestrictSilver(Mobile a, Mobile b) => a.Account != null && a.Account == b.Account;
}
