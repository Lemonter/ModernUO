/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/CityLoyaltySystem.cs).
///
///     Structural changes:
///     <list type="bullet">
///         <item>
///             ServUO's <c>PointsSystem</c>/<c>PointsEntry</c> framework does not exist in this codebase. Each city
///             keeps its own <c>Dictionary&lt;PlayerMobile, CityLoyaltyEntry&gt;</c> here and everything is saved by
///             <see cref="CityLoyaltyPersistence" /> (a <see cref="GenericPersistence" />, the same pattern as
///             <c>Engines/VvV/VvVPersistence.cs</c>) instead of the points framework's save file.
///         </item>
///         <item>
///             <c>Config.Get</c> (RunUO helper) does not exist — replaced with <c>ServerConfiguration.GetSetting</c>.
///         </item>
///         <item>
///             <c>Siege.SiegeShard</c> is not available here, so <see cref="SystemMap" /> is always Trammel.
///         </item>
///         <item>
///             <c>EventSink.Login</c> became the code-generated <c>PlayerMobile.PlayerLoginEvent</c> hook.
///         </item>
///         <item>
///             Reward titles (<c>PlayerMobile.AddRewardTitle</c>/<c>RemoveRewardTitle</c>) have no equivalent here;
///             <see cref="CityLoyaltyEntry.Titles" /> is persisted data only.
///         </item>
///         <item>
///             <c>DateTime.UtcNow</c> for deadlines became <c>Core.Now</c>.
///         </item>
///         <item>
///             <c>ColUtility.Free</c> and <c>IPooledEnumerable</c> dropped (direct enumeration).
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Commands;
using Server.Engines.BuffIcons;
using Server.Gumps;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.CityLoyalty;

[PropertyObject]
public class CityLoyaltySystem
{
    public static readonly bool Enabled = ServerConfiguration.GetSetting("cityLoyalty.enabled", true);
    public static readonly int CitizenJoinWait = ServerConfiguration.GetSetting("cityLoyalty.joinWait", 7);
    public static readonly int BannerCost = ServerConfiguration.GetSetting("cityLoyalty.bannerCost", 250000);
    public static readonly int BannerCooldownDuration = ServerConfiguration.GetSetting("cityLoyalty.bannerCooldown", 24);
    public static readonly int TradeDealCostPeriod = ServerConfiguration.GetSetting("cityLoyalty.tradeDealPeriod", 7);
    public static readonly int TradeDealCooldown = ServerConfiguration.GetSetting("cityLoyalty.tradeDealCooldown", 7);
    public static readonly int TradeDealCost = ServerConfiguration.GetSetting("cityLoyalty.tradeDealCost", 2000000);
    public static readonly int TradeDealUtilizationPeriod =
        ServerConfiguration.GetSetting("cityLoyalty.tradeDealUtilizationPeriod", 24);
    public static readonly int MaxBallotBoxes = ServerConfiguration.GetSetting("cityLoyalty.maxBallotBoxes", 10);
    public static readonly int AnnouncementPeriod = ServerConfiguration.GetSetting("cityLoyalty.announcementPeriod", 48);

    public static readonly TimeSpan LoveAtrophyDuration = TimeSpan.FromHours(40);

    public static int[][] _LoveHatePointsTable =
    {
        new[] { 250 },                                 // Tier 1
        new[] { 500, 1000 },                           // Tier 2
        new[] { 5000, 10000, 25000 },                  // Tier 3
        new[] { 100000, 250000, 500000, 1000000 }      // Tier 4
    };

    public static int[][] _NuetralPointsTable =
    {
        new[] { 250 },        // Tier 1
        new[] { 1000 },       // Tier 2
        new[] { 25000 },      // Tier 3
        new[] { 1000000 }     // Tier 4
    };

    public static LoyaltyRating[][] _LoveLoyaltyTable =
    {
        new[] { LoyaltyRating.Commended },                                                   // Tier 1
        new[] { LoyaltyRating.Esteemed, LoyaltyRating.Respected },                           // Tier 2
        new[] { LoyaltyRating.Honored, LoyaltyRating.Admired, LoyaltyRating.Adored },        // Tier 3
        new[] { LoyaltyRating.Lauded, LoyaltyRating.Exalted, LoyaltyRating.Revered, LoyaltyRating.Venerated } // Tier 4
    };

    public static LoyaltyRating[][] _HateLoyaltyTable =
    {
        new[] { LoyaltyRating.Disfavored },                                                     // Tier 1
        new[] { LoyaltyRating.Disliked, LoyaltyRating.Detested },                               // Tier 2
        new[] { LoyaltyRating.Loathed, LoyaltyRating.Despised, LoyaltyRating.Reviled },         // Tier 3
        new[] { LoyaltyRating.Scorned, LoyaltyRating.Shunned, LoyaltyRating.Villified, LoyaltyRating.Abhorred } // Tier 4
    };

    public static LoyaltyRating[][] _NuetralLoyaltyTable =
    {
        new[] { LoyaltyRating.Doubted },      // Tier 1
        new[] { LoyaltyRating.Distrusted },   // Tier 2
        new[] { LoyaltyRating.Disgraced },    // Tier 3
        new[] { LoyaltyRating.Denigrated }    // Tier 4
    };

    private static readonly ILogger Logger = LogFactory.GetLogger(typeof(CityLoyaltySystem));

    private static DateTime _nextAtrophy;
    private static CityLoyaltyPersistence _persistence;

    private int _completedTrades;
    private Mobile _governor;
    private Mobile _governorElect;
    private bool _pendingGovernor;
    private long _treasury;
    private TradeDeal _activeTradeDeal;

    private Dictionary<PlayerMobile, CityLoyaltyEntry> _playerTable = new();

    public CityLoyaltySystem(City city)
    {
        City = city;
        Election = new CityElection(this);
        CitizenWait = new Dictionary<Mobile, DateTime>();

        Cities ??= new List<CityLoyaltySystem>();
        Cities.Add(this);
    }

    public static Map SystemMap => Map.Trammel;

    public static List<CityLoyaltySystem> Cities { get; private set; }

    public static Moonglow Moonglow { get; set; }
    public static Britain Britain { get; set; }
    public static Jhelom Jhelom { get; set; }
    public static Yew Yew { get; set; }
    public static Minoc Minoc { get; set; }
    public static Trinsic Trinsic { get; set; }
    public static SkaraBrae SkaraBrae { get; set; }
    public static NewMagincia NewMagincia { get; set; }
    public static Vesper Vesper { get; set; }

    public static CityTradeSystem CityTrading { get; set; }

    public Dictionary<PlayerMobile, CityLoyaltyEntry> PlayerTable => _playerTable;

    public Dictionary<Mobile, DateTime> CitizenWait { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public City City { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CityDefinition Definition { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CityElection Election { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime TradeDealStart { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextTradeDealCheck { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool CanUtilize { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public GuardCaptain Captain { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CityHerald Herald { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public TradeMinister Minister { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CityStone Stone { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CityMessageBoard Board { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string Headline { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string Body { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime PostedOn { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile GovernorElect
    {
        get => _governorElect;
        set
        {
            if (value != null && Governor != null)
            {
                Governor = null;
            }

            _governorElect = value;
            Stone?.InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Governor
    {
        get => _governor;
        set
        {
            if (_governor != null && _governor != value && _governor.NetState != null)
            {
                // King Blackthorn thanks you for your service. You have been removed from the Office of the Governor.
                _governor.SendLocalizedMessage(1154071);
            }

            if (value == _governorElect)
            {
                _governorElect = null;
            }

            if (value != null && value != _governor)
            {
                // Hear Ye! Hear Ye! ~1_NAME~ hath accepted the Office of Governor!
                HeraldMessage(1154070, value.Name);
            }

            _pendingGovernor = false;
            _governor = value;

            Stone?.InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool PendingGovernor
    {
        get => _pendingGovernor;
        set
        {
            if (value)
            {
                _governorElect = null;
            }

            _pendingGovernor = value;
            Stone?.InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public long Treasury
    {
        get => _treasury;
        set
        {
            _treasury = value;
            Stone?.InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public TradeDeal ActiveTradeDeal
    {
        get => _activeTradeDeal;
        set
        {
            _activeTradeDeal = value;
            Stone?.InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int CompletedTrades
    {
        get => _completedTrades;
        set
        {
            _completedTrades = value;
            Stone?.InvalidateProperties();
        }
    }

    public override string ToString() => City.ToString();

    public static void Configure()
    {
        ConstructSystems();
        _persistence = new CityLoyaltyPersistence();
    }

    public static void Initialize()
    {
        if (!Enabled)
        {
            return;
        }

        Timer.DelayCall(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), OnTick);

        CommandSystem.Register("ElectionStartTime", AccessLevel.Administrator, e =>
        {
            if (e.Mobile is PlayerMobile pm)
            {
                pm.SendGump(new ElectionStartTimeGump(pm));
            }
        });

        CommandSystem.Register(
            "RemoveWait",
            AccessLevel.Administrator,
            e =>
            {
                foreach (var city in Cities)
                {
                    city.RemoveWaitTime(e.Mobile);
                }
            }
        );

        CommandSystem.Register("SystemInfo", AccessLevel.Administrator, e =>
        {
            if (e.Mobile is PlayerMobile pm)
            {
                pm.SendGump(new SystemInfoGump(pm));
            }
        });

        CityTradeSystem.Initialize();
    }

    public static void ConstructSystems()
    {
        Cities = new List<CityLoyaltySystem>();

        Moonglow = new Moonglow();
        Britain = new Britain();
        Jhelom = new Jhelom();
        Yew = new Yew();
        Minoc = new Minoc();
        Trinsic = new Trinsic();
        SkaraBrae = new SkaraBrae();
        NewMagincia = new NewMagincia();
        Vesper = new Vesper();

        CityTrading = new CityTradeSystem();
    }

    public static CityLoyaltySystem GetCityInstance(City city) => city switch
    {
        City.Moonglow    => Moonglow,
        City.Britain     => Britain,
        City.Jhelom      => Jhelom,
        City.Yew         => Yew,
        City.Minoc       => Minoc,
        City.Trinsic     => Trinsic,
        City.SkaraBrae   => SkaraBrae,
        City.NewMagincia => NewMagincia,
        City.Vesper      => Vesper,
        _                => null
    };

    public static bool IsSetup() => Cities != null && Cities.Exists(c => c.CanUtilize);

    public static CityLoyaltySystem GetCitizenship(Mobile from, bool staffIsCitizen = true)
    {
        if (Cities == null)
        {
            return null;
        }

        return Cities.Find(sys => sys.IsCitizen(from, staffIsCitizen));
    }

    public static bool HasCitizenship(Mobile from)
    {
        if (Cities == null)
        {
            return false;
        }

        return Cities.Exists(sys => sys.IsCitizen(from));
    }

    public static bool HasCitizenship(Mobile from, City city)
    {
        if (Cities == null)
        {
            return false;
        }

        var sys = Cities.Find(s => s.City == city);

        return sys != null && sys.IsCitizen(from);
    }

    public static bool HasTradeDeal(Mobile m, TradeDeal deal)
    {
        var sys = GetCitizenship(m, false);

        if (sys == null)
        {
            return false;
        }

        var entry = sys.GetPlayerEntry(m);

        return sys.ActiveTradeDeal == deal && entry != null && entry.UtilizingTradeDeal;
    }

    public static bool CanAddCitizen(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        return Cities.TrueForAll(city => city.CanAdd(from));
    }

    public static int NextJoinCity(Mobile from)
    {
        foreach (var city in Cities)
        {
            if (!city.CanAdd(from))
            {
                return city.NextJoin(from);
            }
        }

        return 0;
    }

    public static void OnTradeComplete(Mobile from, TradeEntry entry)
    {
        var dest = GetCityInstance(entry.Destination);
        var origin = GetCityInstance(entry.Origin);
        var gold = entry.CalculateGold();

        if (gold > 0 && origin != null)
        {
            origin.AddToTreasury(from, gold);

            // ~1_val~ gold has been deposited into the ~2_NAME~ City treasury for your efforts!
            from.SendLocalizedMessage(1154761, $"{gold:N0}\t{origin.Definition.Name}");
        }

        if (entry.Distance > 0 && origin != null && dest != null)
        {
            origin.AwardLove(from, 150);
            dest.AwardLove(from, 150);
        }

        if (origin != null)
        {
            origin.CompletedTrades++;
        }
    }

    public static void OnSlimTradeComplete(Mobile from, TradeEntry entry)
    {
        var dest = GetCityInstance(entry.Destination);
        var origin = GetCityInstance(entry.Origin);

        if (entry.Distance <= 0 || origin == null || dest == null)
        {
            return;
        }

        origin.AwardHate(from, 25);
        dest.AwardHate(from, 25);
    }

    public static void OnBODTurnIn(Mobile from, int gold)
    {
        if (!Enabled)
        {
            return;
        }

        var city = Cities?.Find(c => c.Definition?.Region != null && c.Definition.Region.IsPartOf(from.Region));

        city?.AwardLove(from, Math.Max(10, gold / 100));
    }

    public static void ActivateTradeDeal(Mobile m, TradeDeal deal)
    {
        switch (deal)
        {
            case TradeDeal.OrderOfEngineers:
                m.AddStatMod(new StatMod(StatType.Dex, $"TradeDeal_{StatType.Dex}", 3, TimeSpan.Zero));
                break;
            case TradeDeal.MiningCooperative:
                m.AddStatMod(new StatMod(StatType.Str, $"TradeDeal_{StatType.Str}", 3, TimeSpan.Zero));
                break;
            case TradeDeal.LeagueOfRangers:
                m.AddStatMod(new StatMod(StatType.Int, $"TradeDeal_{StatType.Int}", 3, TimeSpan.Zero));
                break;
        }
    }

    public static void RemoveTradeDeal(Mobile m)
    {
        m.RemoveStatMod($"TradeDeal_{StatType.Dex}");
        m.RemoveStatMod($"TradeDeal_{StatType.Str}");
        m.RemoveStatMod($"TradeDeal_{StatType.Int}");
    }

    public static City GetRandomCity() => Utility.Random(11) switch
    {
        0     => City.Moonglow,
        1     => City.Britain,
        2     => City.Jhelom,
        3     => City.Yew,
        4     => City.Minoc,
        5     => City.Trinsic,
        6     => City.SkaraBrae,
        7     => City.NewMagincia,
        10    => City.Vesper,
        _     => City.Moonglow
    };

    public static int GetTitleLocalization(Mobile from, CityTitle title, City city) =>
        1152739 + (int)city * 16 + TitleIndex(title, from?.Female ?? false);

    private static int TitleIndex(CityTitle title, bool female) => title switch
    {
        CityTitle.Knight   => female ? 3 : 2,
        CityTitle.Baronet  => female ? 5 : 4,
        CityTitle.Baron    => female ? 7 : 6,
        CityTitle.Viscount => female ? 9 : 8,
        CityTitle.Earl     => female ? 11 : 10,
        CityTitle.Marquis  => female ? 13 : 12,
        CityTitle.Duke     => female ? 15 : 14,
        _                  => female ? 1 : 0
    };

    public static int BannerLocalization(City city) => city switch
    {
        City.Moonglow    => 1098171,
        City.Britain     => 1098172,
        City.Jhelom      => 1098173,
        City.Yew         => 1098174,
        City.Minoc       => 1098175,
        City.Trinsic     => 1098170,
        City.SkaraBrae   => 1098178,
        City.NewMagincia => 1098177,
        City.Vesper      => 1098176,
        _                => 0
    };

    public static int GetCityLocalization(City city) => city switch
    {
        City.Moonglow    => 1011344,
        City.Britain     => 1011028,
        City.Jhelom      => 1011343,
        City.Yew         => 1011032,
        City.Minoc       => 1011031,
        City.Trinsic     => 1011029,
        City.SkaraBrae   => 1011347,
        City.NewMagincia => 1011345,
        City.Vesper      => 1011030,
        _                => 0
    };

    public static int CityLocalization(City city) => GetCityInstance(city)?.Definition.LocalizedName ?? 0;

    public static int RatingLocalization(LoyaltyRating rating) => rating switch
    {
        LoyaltyRating.Disfavored  => 1152118,
        LoyaltyRating.Disliked    => 1152122,
        LoyaltyRating.Detested    => 1152123,
        LoyaltyRating.Loathed     => 1152128,
        LoyaltyRating.Despised    => 1152129,
        LoyaltyRating.Reviled     => 1152130,
        LoyaltyRating.Scorned     => 1152136,
        LoyaltyRating.Shunned     => 1152137,
        LoyaltyRating.Villified   => 1152138,
        LoyaltyRating.Abhorred    => 1152139,
        LoyaltyRating.Doubted     => 1152117,
        LoyaltyRating.Distrusted  => 1152121,
        LoyaltyRating.Disgraced   => 1152127,
        LoyaltyRating.Denigrated  => 1152135,
        LoyaltyRating.Commended   => 1152116,
        LoyaltyRating.Esteemed    => 1152120,
        LoyaltyRating.Respected   => 1152119,
        LoyaltyRating.Honored     => 1152126,
        LoyaltyRating.Admired     => 1152125,
        LoyaltyRating.Adored      => 1152124,
        LoyaltyRating.Lauded      => 1152134,
        LoyaltyRating.Exalted     => 1152133,
        LoyaltyRating.Revered     => 1152132,
        LoyaltyRating.Venerated   => 1152131,
        _                         => 1152115
    };

    public static bool IsLove(LoyaltyRating rating)
    {
        foreach (var ratings in _LoveLoyaltyTable)
        {
            foreach (var r in ratings)
            {
                if (r == rating)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool ApplyCityTitle(PlayerMobile pm, IPropertyList list, string prefix, int loc)
    {
        if (loc == 1154017)
        {
            var city = GetCitizenship(pm);
            var entry = city?.GetPlayerEntry(pm);

            if (entry != null && !string.IsNullOrEmpty(entry.CustomTitle))
            {
                prefix = $"{prefix} {pm.Name} the {entry.CustomTitle}";
                list.Add(1154017, $"{prefix}\t{city.Definition.Name}"); // ~1_TITLE~ of ~2_CITY~
                return true;
            }
        }
        else
        {
            list.Add(1151487, $"{prefix} \t{pm.Name} the \t#{loc}"); // ~1NT_PREFIX~~2NT_NAME~~3NT_SUFFIX~
            return true;
        }

        return false;
    }

    public static bool HasCustomTitle(PlayerMobile pm, out string str)
    {
        var city = GetCitizenship(pm);
        var entry = city?.GetPlayerEntry(pm);

        str = entry != null && !string.IsNullOrEmpty(entry.CustomTitle)
            ? $"{entry.CustomTitle}\t{city.Definition.Name}"
            : null;

        return str != null;
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (!Enabled || pm == null)
        {
            return;
        }

        var sys = GetCitizenship(pm);

        if (sys == null)
        {
            return;
        }

        if (sys.ActiveTradeDeal != TradeDeal.None)
        {
            var entry = sys.GetPlayerEntry(pm);

            if (entry is { UtilizingTradeDeal: true })
            {
                (pm as PlayerMobile)?.AddBuff(
                    new BuffInfo(
                        BuffIcon.CityTradeDeal,
                        1154168,
                        1154169,
                        TimeSpan.Zero,
                        TextDefinition.Of((int)sys.ActiveTradeDeal),
                        true
                    )
                );
                ActivateTradeDeal(pm, sys.ActiveTradeDeal);
            }
        }

        // The moons of Trammel and Felucca align/fail to preserve your virtue status and city loyalty.
        var message = pm.LastOnline + LoveAtrophyDuration > Core.Now ? 1152913 : 1152912;

        Timer.DelayCall(TimeSpan.FromSeconds(0.7), () => pm.SendLocalizedMessage(message));
    }

    public static void OnTick()
    {
        foreach (var sys in Cities)
        {
            var list = new List<Mobile>(sys.CitizenWait.Keys);

            foreach (var m in list)
            {
                if (sys.CitizenWait[m] < Core.Now)
                {
                    sys.RemoveWaitTime(m);
                }
            }

            if (Core.Now > _nextAtrophy)
            {
                _nextAtrophy = Core.Now + TimeSpan.FromDays(1);
            }

            if (sys.NextTradeDealCheck != DateTime.MinValue && sys.NextTradeDealCheck < Core.Now)
            {
                if (sys.Treasury >= TradeDealCost)
                {
                    sys.Treasury -= TradeDealCost;
                    sys.NextTradeDealCheck = Core.Now + TimeSpan.FromDays(TradeDealCostPeriod);
                }
                else
                {
                    sys.OnNewTradeDeal(TradeDeal.None);
                }
            }

            foreach (var entry in sys.PlayerTable.Values)
            {
                entry.CheckTradeDeal();
            }

            sys.Election ??= new CityElection(sys);
            sys.Election.OnTick();

            sys.Stone?.InvalidateProperties();
        }

        CityTradeSystem.OnTick();
    }

    public bool IsGovernor(Mobile m) => m.AccessLevel > AccessLevel.GameMaster || m == Governor;

    public CityLoyaltyEntry GetPlayerEntry(Mobile from, bool create = false)
    {
        if (from is not PlayerMobile pm)
        {
            return null;
        }

        if (_playerTable.TryGetValue(pm, out var entry))
        {
            return entry;
        }

        if (!create)
        {
            return null;
        }

        entry = new CityLoyaltyEntry(pm, City);
        _playerTable[pm] = entry;
        return entry;
    }

    public bool IsCitizen(Mobile from, bool staffIsCitizen = true)
    {
        if (from == null)
        {
            return false;
        }

        if (from.AccessLevel > AccessLevel.Player && staffIsCitizen)
        {
            return true;
        }

        var entry = GetPlayerEntry(from);

        return entry != null && entry.IsCitizen;
    }

    public int GetCitizenCount()
    {
        var count = 0;

        foreach (var entry in _playerTable.Values)
        {
            if (entry.IsCitizen)
            {
                count++;
            }
        }

        return count;
    }

    public void DeclareCitizen(Mobile from) => GetPlayerEntry(from, true)?.DeclareCitizenship();

    public void RenounceCitizenship(Mobile from)
    {
        var entry = GetPlayerEntry(from, true);

        if (entry == null)
        {
            return;
        }

        entry.RenounceCitizenship();

        if (from == Governor)
        {
            _governor = null;
            Stone?.InvalidateProperties();
        }

        if (from == GovernorElect)
        {
            _governorElect = null;
            Stone?.InvalidateProperties();
        }

        CitizenWait[from] = Core.Now + TimeSpan.FromDays(CitizenJoinWait);
    }

    public virtual void AwardHate(Mobile from, double hate)
    {
        var entry = GetPlayerEntry(from, true);

        if (entry == null)
        {
            return;
        }

        if (entry.Love > 10)
        {
            var convert = entry.Hate / 75;
            entry.Love -= convert;
            entry.Hate += convert;
        }

        entry.Hate += (int)hate;

        if (entry.ShowGainMessage)
        {
            // Your deeds in the city of ~1_name~ are worthy of censure.
            from.SendLocalizedMessage(1152321, Definition.Name);
        }

        if (from == Governor && entry.LoyaltyRating < LoyaltyRating.Unknown)
        {
            Governor = null;
        }

        if (from == GovernorElect && entry.LoyaltyRating < LoyaltyRating.Unknown)
        {
            GovernorElect = null;
        }
    }

    public virtual void AwardLove(Mobile from, double love, bool message = true)
    {
        var entry = GetPlayerEntry(from, true);

        if (entry == null)
        {
            return;
        }

        if (entry.Hate > 10)
        {
            var convert = entry.Hate / 75;
            entry.Neutrality += convert;
            entry.Hate -= convert;
        }

        if (message && entry.ShowGainMessage)
        {
            // Your deeds in the city of ~1_name~ are worthy of praise.
            from.SendLocalizedMessage(1152320, Definition.Name);
        }

        entry.Love += (int)love;
    }

    public virtual LoyaltyRating GetLoyaltyRating(Mobile from) => GetLoyaltyRating(from, GetPlayerEntry(from));

    public virtual LoyaltyRating GetLoyaltyRating(Mobile from, CityLoyaltyEntry entry)
    {
        if (entry == null)
        {
            return LoyaltyRating.Unknown;
        }

        int love = entry.Love, hate = entry.Hate, neut = entry.Neutrality;

        if (hate > 0 && hate > love && hate > neut)
        {
            return GetHateRating(hate);
        }

        if (neut > 0 && neut > love && neut > hate)
        {
            return GetNeutralRating(neut);
        }

        return love > 0 ? GetLoveRating(love) : LoyaltyRating.Unknown;
    }

    public virtual LoyaltyRating GetHateRating(int points) => GetRating(points, _LoveHatePointsTable, _HateLoyaltyTable);

    public virtual LoyaltyRating GetNeutralRating(int points) =>
        GetRating(points, _NuetralPointsTable, _NuetralLoyaltyTable);

    public virtual LoyaltyRating GetLoveRating(int points) => GetRating(points, _LoveHatePointsTable, _LoveLoyaltyTable);

    private LoyaltyRating GetRating(int points, int[][] table, LoyaltyRating[][] loyaltyTable)
    {
        var rating = LoyaltyRating.Unknown;

        for (var i = 0; i < table.Length; i++)
        {
            for (var j = 0; j < table[i].Length; j++)
            {
                if (points >= table[i][j])
                {
                    rating = loyaltyTable[i][j];
                }
            }
        }

        return rating;
    }

    public void AddToTreasury(Mobile m, int amount, bool givelove = false)
    {
        Treasury += amount;

        if (givelove && m != null)
        {
            AwardLove(m, amount / 250);
        }
    }

    public virtual bool HasTitle(Mobile from, CityTitle title)
    {
        var entry = GetPlayerEntry(from);

        return entry != null && (entry.Titles & title) != 0;
    }

    public virtual void AddTitle(Mobile from, CityTitle title) => GetPlayerEntry(from)?.AddTitle(title);

    public int GetTitleCost(CityTitle title) => title switch
    {
        CityTitle.Knight   => 10000,
        CityTitle.Baronet  => 100000,
        CityTitle.Baron    => 1000000,
        CityTitle.Viscount => 2000000,
        CityTitle.Earl     => 5000000,
        CityTitle.Marquis  => 10000000,
        CityTitle.Duke     => 50000000,
        _                  => 0
    };

    public bool HasMinRating(Mobile from, CityTitle title)
    {
        var entry = GetPlayerEntry(from);

        return entry != null && entry.LoyaltyRating >= GetMinimumRating(title);
    }

    public LoyaltyRating GetMinimumRating(CityTitle title) => title switch
    {
        CityTitle.Knight   => LoyaltyRating.Commended,
        CityTitle.Baronet  => LoyaltyRating.Esteemed,
        CityTitle.Baron    => LoyaltyRating.Respected,
        CityTitle.Viscount => LoyaltyRating.Admired,
        CityTitle.Earl     => LoyaltyRating.Adored,
        CityTitle.Marquis  => LoyaltyRating.Revered,
        CityTitle.Duke     => LoyaltyRating.Venerated,
        _                  => LoyaltyRating.Disfavored
    };

    public void OnNewTradeDeal(TradeDeal newTradeDeal)
    {
        if (ActiveTradeDeal == TradeDeal.None)
        {
            NextTradeDealCheck = Core.Now + TimeSpan.FromDays(TradeDealCostPeriod);
        }
        else if (newTradeDeal == TradeDeal.None)
        {
            NextTradeDealCheck = DateTime.MinValue;
            TradeDealStart = DateTime.MinValue;
        }
        else
        {
            TradeDealStart = Core.Now;
        }

        ActiveTradeDeal = newTradeDeal;

        Stone?.InvalidateProperties();

        foreach (var player in PlayerTable.Values)
        {
            if (player.UtilizingTradeDeal)
            {
                player.UtilizingTradeDeal = false;
            }
        }
    }

    public void TryUtilizeTradeDeal(Mobile m)
    {
        if (GetPlayerEntry(m, true) is not { } entry)
        {
            return;
        }

        if (ActiveTradeDeal == TradeDeal.None)
        {
            // The City doth nay currently have a Trade Deal in place...
            HeraldMessage(m, 1154064);
        }
        else if (entry.UtilizingTradeDeal)
        {
            // Thou hath already claimed the benefit of the Trade Deal today!
            HeraldMessage(m, 1154063);
        }
        else if (entry.LoyaltyRating < LoyaltyRating.Respected)
        {
            // Begging thy pardon, but thou must be at least Respected within the City...
            HeraldMessage(m, 1154062);
        }
        else
        {
            entry.UtilizingTradeDeal = true;
            (m as PlayerMobile)?.AddBuff(
                new BuffInfo(
                    BuffIcon.CityTradeDeal,
                    1154168,
                    1154169,
                    TimeSpan.Zero,
                    TextDefinition.Of((int)ActiveTradeDeal),
                    true
                )
            );

            ActivateTradeDeal(m, ActiveTradeDeal);

            m.Delta(MobileDelta.WeaponDamage);

            // You gain the benefit of your City's Trade Deal!
            m.SendLocalizedMessage(1154075);
        }
    }

    public void HeraldMessage(int message) => HeraldMessage(message, "");

    public void HeraldMessage(string message) => Herald?.Say(message);

    public void HeraldMessage(int message, string args) => Herald?.Say(message, args);

    public void HeraldMessage(Mobile to, int message)
    {
        if (Herald != null)
        {
            Herald.SayTo(to, message);
        }
        else
        {
            to.SendLocalizedMessage(message);
        }
    }

    public bool CanAdd(Mobile from)
    {
        if (!CitizenWait.TryGetValue(from, out var wait))
        {
            return true;
        }

        if (wait < Core.Now)
        {
            RemoveWaitTime(from);
            return true;
        }

        return false;
    }

    public int NextJoin(Mobile from) =>
        CitizenWait.TryGetValue(from, out var wait) ? (int)(wait - Core.Now).TotalDays : 0;

    public void RemoveWaitTime(Mobile from) => CitizenWait.Remove(from);

    public static void SerializeState(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        foreach (var sys in Cities)
        {
            writer.Write((int)sys.City);
            writer.Write(sys.Governor);
            writer.Write(sys._governorElect);
            writer.Write(sys._pendingGovernor);
            writer.Write(sys._treasury);
            writer.Write((int)sys._activeTradeDeal);
            writer.Write(sys.TradeDealStart);
            writer.Write(sys.NextTradeDealCheck);
            writer.Write(sys.CanUtilize);
            writer.Write(sys._completedTrades);
            writer.Write(sys.Headline);
            writer.Write(sys.Body);
            writer.Write(sys.PostedOn);

            writer.Write(sys.CitizenWait.Count);
            foreach (var (m, dt) in sys.CitizenWait)
            {
                writer.Write(m);
                writer.Write(dt);
            }

            sys.Election?.Serialize(writer);

            writer.Write(sys._playerTable.Count);
            foreach (var (pm, entry) in sys._playerTable)
            {
                writer.Write(pm);
                entry.Persist(writer);
            }
        }

        CityTradeSystem.SerializeState(writer);
    }

    public static void DeserializeState(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        foreach (var sys in Cities)
        {
            reader.ReadInt(); // City

            sys._governor = reader.ReadEntity<Mobile>();
            sys._governorElect = reader.ReadEntity<Mobile>();
            sys._pendingGovernor = reader.ReadBool();
            sys._treasury = reader.ReadLong();
            sys._activeTradeDeal = (TradeDeal)reader.ReadInt();
            sys.TradeDealStart = reader.ReadDateTime();
            sys.NextTradeDealCheck = reader.ReadDateTime();
            sys.CanUtilize = reader.ReadBool();
            sys._completedTrades = reader.ReadInt();
            sys.Headline = reader.ReadString();
            sys.Body = reader.ReadString();
            sys.PostedOn = reader.ReadDateTime();

            var waitCount = reader.ReadInt();
            for (var i = 0; i < waitCount; i++)
            {
                var m = reader.ReadEntity<Mobile>();
                var dt = reader.ReadDateTime();

                if (m != null && dt > Core.Now)
                {
                    sys.CitizenWait[m] = dt;
                }
            }

            sys.Election ??= new CityElection(sys);
            sys.Election.Deserialize(reader);

            var entryCount = reader.ReadInt();
            for (var i = 0; i < entryCount; i++)
            {
                var pm = reader.ReadEntity<Mobile>() as PlayerMobile;
                var entry = CityLoyaltyEntry.Load(reader);

                if (pm == null)
                {
                    continue;
                }

                entry.SetPlayer(pm);
                entry.CheckTradeDeal();
                sys._playerTable[pm] = entry;
            }
        }

        CityTradeSystem.DeserializeState(reader);

        Logger.Information("City Loyalty system loaded");
    }
}

// Ported from ServUO (Scripts/Services/City Loyalty System/CityLoyaltySystem.cs, city subclasses).
public class Moonglow : CityLoyaltySystem
{
    public Moonglow() : base(City.Moonglow) => Definition = new CityDefinition(
        City.Moonglow,
        "Moonglow",
        new Point3D(4480, 1173, 0),
        new Point3D(4416, 1044, -2),
        new Point3D(4480, 1172, 0),
        new Point3D(4551, 1051, 0),
        new Point3D(4478, 1170, 0),
        "Мунглоу",
        1114143,
        1154524
    );
}

public class Britain : CityLoyaltySystem
{
    public Britain() : base(City.Britain) => Definition = new CityDefinition(
        City.Britain,
        "Britain",
        new Point3D(1445, 1694, 0),
        new Point3D(1436, 1760, -2),
        new Point3D(1446, 1694, 0),
        new Point3D(1417, 1715, 20),
        new Point3D(1481, 1718, 0),
        "Британия",
        1114148,
        1154521
    );
}

public class Jhelom : CityLoyaltySystem
{
    public Jhelom() : base(City.Jhelom) => Definition = new CityDefinition(
        City.Jhelom,
        "Jhelom",
        new Point3D(1336, 3769, 0),
        new Point3D(1377, 3879, 0),
        new Point3D(1336, 3770, 0),
        new Point3D(1379, 3797, 0),
        new Point3D(1333, 3776, 0),
        "Джелом",
        1114146,
        1154522
    );
}

public class Yew : CityLoyaltySystem
{
    public Yew() : base(City.Yew) => Definition = new CityDefinition(
        City.Yew,
        "Yew",
        new Point3D(632, 863, 0),
        new Point3D(621, 1043, 0),
        new Point3D(631, 863, 0),
        new Point3D(385, 914, 0),
        new Point3D(626, 863, 0),
        "Ию",
        1114138,
        1154529
    );
}

public class Minoc : CityLoyaltySystem
{
    public Minoc() : base(City.Minoc) => Definition = new CityDefinition(
        City.Minoc,
        "Minoc",
        new Point3D(2514, 558, 0),
        new Point3D(2499, 398, 15),
        new Point3D(2514, 559, 0),
        new Point3D(2424, 533, 0),
        new Point3D(2522, 558, 0),
        "Минок",
        1114139,
        1154523
    );
}

public class Trinsic : CityLoyaltySystem
{
    public Trinsic() : base(City.Trinsic) => Definition = new CityDefinition(
        City.Trinsic,
        "Trinsic",
        new Point3D(1907, 2682, 0),
        new Point3D(2061, 2855, -2),
        new Point3D(1907, 2683, 0),
        new Point3D(1851, 2772, 0),
        new Point3D(1907, 2679, 0),
        "Тринсик",
        1114142,
        1154527
    );
}

public class SkaraBrae : CityLoyaltySystem
{
    public SkaraBrae() : base(City.SkaraBrae) => Definition = new CityDefinition(
        City.SkaraBrae,
        "Skara Brae",
        new Point3D(587, 2153, 0),
        new Point3D(645, 2228, -2),
        new Point3D(586, 2153, 0),
        new Point3D(571, 2210, 0),
        new Point3D(580, 2155, 0),
        "Скара-Брей",
        1114145,
        1154526
    );
}

public class NewMagincia : CityLoyaltySystem
{
    public NewMagincia() : base(City.NewMagincia) => Definition = new CityDefinition(
        City.NewMagincia,
        "Magincia",
        new Point3D(3795, 2247, 20),
        new Point3D(3677, 2254, 20),
        new Point3D(3796, 2247, 20),
        new Point3D(3680, 2269, 26),
        new Point3D(3795, 2259, 20),
        "Маджинция",
        1114141,
        1154525
    );
}

public class Vesper : CityLoyaltySystem
{
    public Vesper() : base(City.Vesper) => Definition = new CityDefinition(
        City.Vesper,
        "Vesper",
        new Point3D(2891, 683, 0),
        new Point3D(3004, 834, 0),
        new Point3D(2891, 682, 0),
        new Point3D(3004, 822, 0),
        new Point3D(2899, 685, 0),
        "Веспер",
        1114140,
        1154528
    );
}
