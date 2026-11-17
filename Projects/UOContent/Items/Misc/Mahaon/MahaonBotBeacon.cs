using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Systems.Bots;

namespace Server.Items;

[SerializationGenerator(3, false)]
public partial class MahaonBotBeacon : Item
{
    [SerializableField(0)]
    private int _totalLimit;

    // Was a single int[5] indexed by BotArchetype — replaced with 5 plain fields after the
    // array form triggered a source-generator bug (CS1501 in the generated Deserialize,
    // "Deserialize doesn't take 2 arguments") that a plain array field elsewhere in the
    // codebase (Meraktus.cs, also version 1) didn't hit, so it wasn't arrays in general,
    // just something about this specific one. Plain ints are the same pattern already
    // proven working for _totalLimit/_thiefTarget right here in this class.
    [SerializableField(1)]
    private int _targetWarrior;

    [SerializableField(2)]
    private int _targetMage;

    [SerializableField(3)]
    private int _targetArcher;

    [SerializableField(4)]
    private int _targetCrafter;

    [SerializableField(5)]
    private int _targetTrader;

    [SerializableField(6)]
    private int _thiefTarget; // separate slot — Thief is a profession inside Trader, not its own archetype

    [SerializableField(7)]
    private string _guildName; // which bot guild (Systems.MahaonBots.BotGuilds) this beacon's spawns join, if any

    [SerializableField(8)]
    private string _cityName; // which city (Systems.MahaonCities.CityControlSystem.Cities) this beacon belongs to, if any — purely informational/for the gump, doesn't move anything

    // Flavor names picked at creation — purely cosmetic, doesn't affect anything else.
    // Pick freely/extend this list, nothing else references it by index or content.
    private static readonly string[] NamePool =
    {
        "Монумент борцам с некромантом", "Памятник павшим у Чёрных Врат",
        "Курган Освободителей", "Обелиск Скорби", "Алтарь Непокорённых",
        "Стела Последнего Рубежа", "Камень Памяти Стражей", "Монолит Изгнавших Тьму",
        "Постамент Героев Пепелища", "Мемориал Битвы у Некрополя",
        "Колонна Победивших Мор", "Плита Забытого Похода",
        "Изваяние Хранителей Света", "Пирамида Развеявших Проклятье",
        "Знак Памяти Отряда Рассвета"
    };

    // Bots owned by this beacon — memory only: each bot's brain remembers its beacon and
    // reclaims its slot on load (BotSystem.Register), so this field needs no serializing.
    private readonly List<PlayerMobile> _ownedBots = new();

    private static readonly List<MahaonBotBeacon> AllBeacons = new();

    public static IReadOnlyList<MahaonBotBeacon> All => AllBeacons;

    /// <summary>
    ///     Насколько широко вокруг маяка нельзя начинать драку.
    ///
    ///     Новые боты появляются у своего маяка и возвращаются к нему домой, а разбойник
    ///     ищет жертв в двадцати тайлах вокруг себя — то есть у того же маяка. Получалась
    ///     ловушка: появившийся бот тут же становился добычей. Нужна тихая зона вокруг
    ///     самой точки. (Призрак бота теперь идёт к лекарю, а не воскресает у маяка.)
    ///
    ///     Это не защита от урона: уже начатую драку сюда можно притащить, и вмешаться в
    ///     неё тоже можно. Запрещено только выбирать здесь новую жертву.
    /// </summary>
    public const int PeaceRadius = 16;

    /// <summary>Стоит ли этот некто вплотную к какому-нибудь маяку.</summary>
    public static bool IsNearAnyBeacon(Mobile m)
    {
        if (m?.Map == null)
        {
            return false;
        }

        for (var i = 0; i < AllBeacons.Count; i++)
        {
            var beacon = AllBeacons[i];

            if (!beacon.Deleted && beacon.Map == m.Map && beacon.GetDistanceToSqrt(m.Location) <= PeaceRadius)
            {
                return true;
            }
        }

        return false;
    }
    // Один бот на маяк за тик: 12 с — пять в минуту. Для проверок под нагрузкой тик ускоряют
    // настройкой bots.beaconSpawnSeconds, не трогая код.
    private static TimeSpan SpawnTick = TimeSpan.FromSeconds(12);

    public static void Configure() =>
        SpawnTick = TimeSpan.FromSeconds(Math.Max(0.1, ServerConfiguration.GetOrUpdateSetting("bots.beaconSpawnSeconds", 12.0)));

    [Constructible]
    public MahaonBotBeacon() : base(0x2D12)
    {
        Movable = false;
        Name = NamePool.RandomElement();
        _totalLimit = 0;

        AllBeacons.Add(this);
        _spawnTimer = Timer.DelayCall(SpawnTick, SpawnTick, TrySpawnOne);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        AllBeacons.Add(this);
        _spawnTimer = Timer.DelayCall(SpawnTick, SpawnTick, TrySpawnOne);
    }

    // Both old versions (0 and 1) still had TargetCounts as a single int[5] indexed by
    // BotArchetype (Warrior, Mage, Archer, Crafter, Trader, in that enum order) — version 2
    // split that into 5 plain named fields (see the class-level comment on _targetWarrior
    // for why). Everything else has a same-named property in both old shapes, so the
    // generator copies those on its own; only the array needs manual unpacking here.
    private void MigrateFrom(V0Content content)
    {
        UnpackTargetCounts(content.TargetCounts);
    }

    private void MigrateFrom(V1Content content)
    {
        UnpackTargetCounts(content.TargetCounts);
    }

    // Version 3 only added CityName on top of everything version 2 already had — every
    // other property has a same-named match, so there's nothing to actually do here, but
    // the generator still requires this method to exist for the v2->v3 step (learned the
    // hard way on this exact class before, with the v0/v1 array-unpacking migrations —
    // apparently that requirement holds even when the migration itself is a no-op).
    private void MigrateFrom(V2Content content)
    {
    }

    private void UnpackTargetCounts(int[] counts)
    {
        if (counts is not { Length: 5 })
        {
            return;
        }

        _targetWarrior = counts[(int)BotArchetype.Warrior];
        _targetMage = counts[(int)BotArchetype.Mage];
        _targetArcher = counts[(int)BotArchetype.Archer];
        _targetCrafter = counts[(int)BotArchetype.Crafter];
        _targetTrader = counts[(int)BotArchetype.Trader];
    }

    public int GetTarget(BotArchetype archetype)
    {
        var raw = archetype switch
        {
            BotArchetype.Warrior => _targetWarrior,
            BotArchetype.Mage    => _targetMage,
            BotArchetype.Archer  => _targetArcher,
            BotArchetype.Crafter => _targetCrafter,
            BotArchetype.Trader  => _targetTrader,
            _                    => 0
        };

        // A guild that doesn't allow crafts gets zero Crafter/Trader spawns from this
        // beacon regardless of whatever the GM configured — "им крафты не нужны, их там и
        // не будет" is a hard rule, not a soft bias.
        if (!string.IsNullOrEmpty(GuildName)
            && (archetype == BotArchetype.Crafter || archetype == BotArchetype.Trader)
            && !Systems.MahaonBots.GuildSpecialization.GetAllowsCrafts(GuildName))
        {
            return 0;
        }

        return raw;
    }

    public void SetTarget(BotArchetype archetype, int value)
    {
        value = Math.Max(0, value);

        switch (archetype)
        {
            case BotArchetype.Warrior:
                _targetWarrior = value;
                break;
            case BotArchetype.Mage:
                _targetMage = value;
                break;
            case BotArchetype.Archer:
                _targetArcher = value;
                break;
            case BotArchetype.Crafter:
                _targetCrafter = value;
                break;
            case BotArchetype.Trader:
                _targetTrader = value;
                break;
        }
    }

    /// <summary>
    ///     Бот, которого маяк потерял: он ещё не удалён, но мир его уже не держит, а
    ///     планировщик снял с учёта — значит думать он больше не будет никогда.
    ///
    ///     Откуда такие берутся. BotBrain.Think отдаёт −1, когда бот оказался вне мира
    ///     (Map == null или Map.Internal), а BotScheduler на −1 делает
    ///     BotSystem.Unregister. Из учёта бот при этом выходит, из МИРА — нет, и маяку
    ///     никто не сообщает. Получается сирота: невидим, не думает, и держит слот вечно.
    ///
    ///     Чем это кончается, видно на живом шарде: маяк насчитал 481 своего бота из 600 и
    ///     встал намертво, все архетипы «заполнены», новых не появляется — а [BotStats
    ///     показывает ноль, потому что в учёте системы нет ни одного. Два счёта разошлись и
    ///     сойтись сами уже не могли.
    ///
    ///     Поэтому слот считается занятым только тем, кто действительно в игре. Сироты
    ///     освобождают места, и маяк оживает без вмешательства руками.
    /// </summary>
    private static bool IsLost(PlayerMobile bot) =>
        bot.Deleted || bot.Map == null || bot.Map == Map.Internal ||
        bot is BotMobile && bot.GetBrain() is not { Registered: true };

    /// <summary>Total bots this beacon currently owns — alive or waiting to respawn. The
    /// limit counts both, so a dead bot still holds its slot until it's actually gone.
    /// A bot the world has lost (see IsLost) holds nothing.</summary>
    public int OwnedCount()
    {
        _ownedBots.RemoveAll(IsLost);
        return _ownedBots.Count;
    }

    public int OwnedCount(BotArchetype archetype)
    {
        _ownedBots.RemoveAll(IsLost);
        var count = 0;

        foreach (var bot in _ownedBots)
        {
            if (bot is BotMobile b && b.Archetype == archetype)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Thieves are Trader-archetype bots with the Thief profession category
    /// specifically — not a whole separate archetype, so this counts a subset of what
    /// OwnedCount(BotArchetype.Trader) counts.</summary>
    public int OwnedThiefCount()
    {
        _ownedBots.RemoveAll(IsLost);
        var count = 0;

        foreach (var bot in _ownedBots)
        {
            if (bot is BotMobile { Archetype: BotArchetype.Trader } &&
                BotMobile.IsProfessionCategory(bot, Systems.MahaonProfessions.ProfessionCategory.Thief))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    ///     В какую гильдию маяк отдаёт конкретного бота.
    ///
    ///     Своя гильдия маяка, если задана, — тогда все его боты в ней одной, это явный
    ///     выбор ГМа. Иначе бот попадает в одну из гильдий своего города, и в какую именно
    ///     — решает его собственный серийник: выбор устойчивый (бот не меняет гильдию от
    ///     перезапуска к перезапуску) и при этом город получается населён несколькими
    ///     гильдиями сразу.
    ///
    ///     Случайный выбор на каждого бота, который тут был раньше, не годится именно
    ///     из-за неустойчивости: бот менял гильдию при каждой перерегистрации, а вместе с
    ///     ней менялись друзья и враги.
    /// </summary>
    public string GuildNameFor(PlayerMobile bot)
    {
        if (!string.IsNullOrEmpty(GuildName))
        {
            return GuildName;
        }

        var cityGuilds = Systems.MahaonBots.BotGuilds.NamesForCity(CityName);

        if (cityGuilds is not { Count: > 0 })
        {
            return Systems.MahaonBots.BotGuilds.NameForBeacon(this);
        }

        return cityGuilds[(int)(bot?.Serial.Value ?? 0) % cityGuilds.Count];
    }

    /// <summary>Гильдии, в которых маяк вправе держать своих ботов — для проверки, не
    /// оказался ли бот в чужой.</summary>
    public List<string> AllowedGuildNames()
    {
        if (!string.IsNullOrEmpty(GuildName))
        {
            return new List<string> { GuildName };
        }

        return Systems.MahaonBots.BotGuilds.NamesForCity(CityName) ??
               new List<string> { Systems.MahaonBots.BotGuilds.NameForBeacon(this) };
    }

    public void Claim(PlayerMobile bot)
    {
        if (!_ownedBots.Contains(bot))
        {
            _ownedBots.Add(bot);
        }

        // Свой угол у каждого горожанина. Раньше домом всех ботов маяка была одна точка у
        // самого маяка, и сотни ботов жили в одной толпе: толкались, обходили друг друга и
        // каждый перебирал сотни соседей. Тот, кто ещё живёт у самого маяка (новый или из
        // старого сохранения), получает дом в округе.
        if (bot.GetBrain() is { } brain && brain.HomeMap == Map && Map != null && Utility.InRange(brain.Home, Location, 4))
        {
            brain.Home = HomeSpot();
        }
    }

    private const int HomeMinRange = 6;
    private const int HomeMaxRange = 28;

    /// <summary>Случайное место в округе маяка, где можно стоять и куда можно дойти.</summary>
    internal Point3D HomeSpot()
    {
        var graph = Engines.Pathing.Nav.NavSystem.GetGraph(Map);
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var dx = Utility.RandomMinMax(-HomeMaxRange, HomeMaxRange);
            var dy = Utility.RandomMinMax(-HomeMaxRange, HomeMaxRange);
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) < HomeMinRange ||
                !Map.CanSpawnMobile(X + dx, Y + dy, Z - 20, Z + 20, false, false, out var z))
            {
                continue;
            }

            var p = new Point3D(X + dx, Y + dy, z);
            if (Multis.BaseHouse.FindHouseAt(p, Map, 16) != null ||
                Systems.MahaonWorld.MahaonCityHouseSystem.Find(p, Map) != null ||
                graph != null && graph.Locate(p.X, p.Y, p.Z, 0) < 0)
            {
                continue;
            }

            return p;
        }

        return Location;
    }

    private void TrySpawnOne()
    {
        if (Deleted)
        {
            AllBeacons.Remove(this);
            return;
        }

        if (Map == null || OwnedCount() >= TotalLimit)
        {
            return;
        }

        // Spawn whichever archetype is furthest below its own target, relative to that
        // target — keeps the mix balanced instead of just filling warriors first every time.
        // Thief is checked as its own extra slot alongside the 5 real archetypes.
        var bestArchetype = (BotArchetype)(-1);
        var bestIsThief = false;
        var bestDeficit = 0.0;

        // Лимит маяка — это сколько ботов он держит. Раньше спаун останавливался на сумме целей по
        // типам, и маяк с лимитом 600 и целями на 481 вечно показывал «481/600», хотя ни одного места
        // больше не выдавал. Теперь цели — доли: при сумме меньше лимита они растягиваются до него.
        var scale = Scale();

        for (var i = 0; i < 5; i++)
        {
            var archetype = (BotArchetype)i;
            var target = (int)Math.Ceiling(GetTarget(archetype) * scale);
            if (target <= 0)
            {
                continue;
            }

            var have = OwnedCount(archetype);
            if (have >= target)
            {
                continue;
            }

            var deficit = (target - have) / (double)target;
            if (deficit > bestDeficit)
            {
                bestDeficit = deficit;
                bestArchetype = archetype;
                bestIsThief = false;
            }
        }

        var thiefTarget = (int)Math.Ceiling(_thiefTarget * scale);
        if (thiefTarget > 0)
        {
            var haveThieves = OwnedThiefCount();
            if (haveThieves < thiefTarget)
            {
                var thiefDeficit = (thiefTarget - haveThieves) / (double)thiefTarget;
                if (thiefDeficit > bestDeficit)
                {
                    bestDeficit = thiefDeficit;
                    bestArchetype = BotArchetype.Trader;
                    bestIsThief = true;
                }
            }
        }

        if ((int)bestArchetype < 0)
        {
            return; // every configured archetype is already at its target
        }

        var spot = FindSpawnSpot();
        var name = Systems.MahaonBots.BotNameGenerator.Generate();
        var bot = BotMobile.Create(name, spot, Map, bestArchetype, forceThief: bestIsThief);

        // CityName doubles as the bot's home city: banking, shopping and the ghost's walk to a
        // healer start from there.
        Systems.Bots.BotSystem.Register(bot, spot, Map, CityName);
        Claim(bot);
        bot.Brain.OwnerBeacon = this;

        // Гильдия у бота всегда есть: своя у маяка, если задана, иначе общая для всех
        // ботов ЭТОГО маяка. Раньше безгильдейный маяк отдавал ботов на волю случайного
        // выбора в BotMobile.ApplyNameTemplate, и соседи по точке возрождения оказывались
        // чужаками друг другу.
        Systems.MahaonBots.BotGuilds.Join(GuildNameFor(bot), bot);
    }

    /// <summary>Во сколько раз растянуть цели по типам, чтобы их сумма дошла до лимита.</summary>
    internal double Scale()
    {
        var sum = 0;
        for (var i = 0; i < 5; i++)
        {
            sum += GetTarget((BotArchetype)i);
        }

        return sum > 0 && sum < TotalLimit ? TotalLimit / (double)sum : 1.0;
    }

    private Point3D FindSpawnSpot()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var dx = Utility.RandomMinMax(-3, 3);
            var dy = Utility.RandomMinMax(-3, 3);
            var loc = new Point3D(X + dx, Y + dy, Z);

            if (Map.CanSpawnMobile(loc))
            {
                return loc;
            }
        }

        return Location;
    }

    // Таймер раньше не хранился и после удаления маяка тикал вечно.
    private Timer _spawnTimer;

    public override void OnDelete()
    {
        _spawnTimer?.Stop();
        _spawnTimer = null;
        AllBeacons.Remove(this);

        // Раньше маяк удалялся, а его боты оставались навсегда — сироты копились
        // годами, отсюда и рост времени сохранения, и зависание при попытке [ClearBots
        // удалить всё разом. Копируем список перед перебором — на случай, если Delete()
        // где-то triggers обратный вызов к самому маяку (не хочу мутировать _ownedBots
        // во время его же перебора).
        var toDelete = new List<PlayerMobile>(_ownedBots);

        foreach (var bot in toDelete)
        {
            if (!bot.Deleted)
            {
                bot.Delete();
            }
        }

        _ownedBots.Clear();
        base.OnDelete();
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendMessage("Только GM может настраивать маяк.");
            return;
        }

        from.SendGump(new MahaonBotBeaconGump(this));
    }
}
