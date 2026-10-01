using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

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

    // Bots owned by this beacon — tracked in memory only, same as the rest of the bot
    // system (BotProfile itself isn't persisted to disk either), so this plain field
    // (not [SerializableField]) is intentional.
    private readonly List<PlayerMobile> _ownedBots = new();

    private static readonly List<MahaonBotBeacon> AllBeacons = new();

    /// <summary>
    ///     Насколько широко вокруг маяка нельзя начинать драку.
    ///
    ///     Боты возрождаются прямо у своего маяка, а разбойник ищет жертв в двадцати
    ///     тайлах вокруг себя — то есть у того же маяка. Получалась ловушка: убитый
    ///     вставал и тут же получал снова. Минуты неприкосновенности после возрождения
    ///     мало, потому что охотник просто ждёт, когда она кончится; нужна именно тихая
    ///     зона вокруг самой точки.
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
    private static readonly TimeSpan SpawnTick = TimeSpan.FromSeconds(12); // 5/min
    private static readonly TimeSpan ResurrectTick = TimeSpan.FromSeconds(4); // much more frequent than spawning

    [Constructible]
    public MahaonBotBeacon() : base(0x2D12)
    {
        Movable = false;
        Name = NamePool.RandomElement();
        _totalLimit = 0;

        AllBeacons.Add(this);
        Timer.DelayCall(SpawnTick, SpawnTick, TrySpawnOne);
        Timer.DelayCall(ResurrectTick, ResurrectTick, TryResurrectOwnedDead);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        AllBeacons.Add(this);
        Timer.DelayCall(SpawnTick, SpawnTick, TrySpawnOne);
        Timer.DelayCall(ResurrectTick, ResurrectTick, TryResurrectOwnedDead);
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

    /// <summary>Total bots this beacon currently owns — alive or waiting to respawn. The
    /// limit counts both, so a dead bot still holds its slot until it's actually gone.</summary>
    public int OwnedCount()
    {
        _ownedBots.RemoveAll(b => b.Deleted);
        return _ownedBots.Count;
    }

    public int OwnedCount(BotArchetype archetype)
    {
        _ownedBots.RemoveAll(b => b.Deleted);
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
        _ownedBots.RemoveAll(b => b.Deleted);
        var count = 0;

        foreach (var bot in _ownedBots)
        {
            if (bot is BotMobile { Archetype: BotArchetype.Trader } &&
                Systems.MahaonBots.BotController.IsProfessionCategory(bot, Systems.MahaonProfessions.ProfessionCategory.Thief))
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
    }

    private void TryResurrectOwnedDead()
    {
        if (Deleted)
        {
            return;
        }

        foreach (var bot in _ownedBots)
        {
            if (bot is BotMobile botMobile && !bot.Deleted && !bot.Alive)
            {
                Systems.MahaonBots.BotController.PerformResurrection(botMobile);
            }
        }
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

        for (var i = 0; i < 5; i++)
        {
            var archetype = (BotArchetype)i;
            var target = GetTarget(archetype);
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

        if (_thiefTarget > 0)
        {
            var haveThieves = OwnedThiefCount();
            if (haveThieves < _thiefTarget)
            {
                var thiefDeficit = (_thiefTarget - haveThieves) / (double)_thiefTarget;
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

        // CityName (informational-only until now) doubles as HomeCity/CurrentCity here —
        // without it every beacon-spawned bot had no city to fall back to, so getting
        // "stuck" while traveling or dying anywhere on the map always teleported/
        // resurrected it right back to this exact beacon tile instead of the nearer city.
        Systems.MahaonBots.BotController.RegisterBot(bot, spot, Map, CityName);
        Claim(bot);

        // Гильдия у бота всегда есть: своя у маяка, если задана, иначе общая для всех
        // ботов ЭТОГО маяка. Раньше безгильдейный маяк отдавал ботов на волю случайного
        // выбора в BotMobile.ApplyNameTemplate, и соседи по точке возрождения оказывались
        // чужаками друг другу.
        Systems.MahaonBots.BotGuilds.Join(GuildNameFor(bot), bot);

        if (Systems.MahaonBots.BotController.TryGetProfile(bot, out var profile))
        {
            profile.OwnerBeacon = this;
        }
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

    public override void OnDelete()
    {
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
