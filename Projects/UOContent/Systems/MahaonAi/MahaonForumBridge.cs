using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;
using Server.Systems.MahaonBots;

namespace Server.Systems.MahaonAi;

/// <summary>
///     Posts real in-game events to the website's forum — see mahaon-control/server-
///     control.js's POST /api/forum/posts on the site side. Same "fire and forget,
///     disabled cleanly if unconfigured" shape as MahaonAiTextGenerator, reusing that
///     exact pattern rather than inventing a second one.
///
///     Deliberately does NOT wait for a response or touch game state afterward — this is
///     one-way (game → website), nothing the website says back ever affects the game, so
///     there's no continuation to marshal back onto the main thread the way
///     MahaonAiTextGenerator needs to for its callback.
///
///     Every post is tied to something that actually happened (a real Adjust() crossing
///     into hostile territory, a real guild relation flipping to War, a real
///     NotableDrop/NotableDeath rumor) — text is templated and flavored by the bot's real
///     PersonalityTrait, picked randomly from a few phrasings per trait so it doesn't
///     read as the same three sentences on repeat, but nothing here invents an event that
///     didn't happen.
/// </summary>
public static class MahaonForumBridge
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(MahaonForumBridge));
    private static HttpClient _httpClient;
    private static string _postUrl;

    public static void Configure()
    {
        // Empty by default — matches MahaonAiTextGenerator's convention. Example value:
        // "http://localhost:3000/mahaon/api/forum/posts" (same host:port as config.json's
        // "port" on the website side, same /mahaon mount point server.js already uses).
        _postUrl = ServerConfiguration.GetOrUpdateSetting("mahaonForum.postUrl", "");

        if (IsEnabled)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _logger.Information($"MahaonForumBridge enabled — posting to {_postUrl}.");
        }
        else
        {
            _logger.Information("MahaonForumBridge disabled (no mahaonForum.postUrl configured).");
        }
    }

    public static bool IsEnabled => !string.IsNullOrEmpty(_postUrl);

    // ---- Предел на поток ----
    //
    // Постинг был мгновенным и безусловным: каждая смерть бота, каждый переход отношений
    // во вражду, каждый объявленный набег уходили на сайт отдельным сообщением, ничем не
    // сдерживаемые. Когда боты сцепились на точке возрождения, это дало три сотни
    // сообщений за пять минут — форум перестал быть читаемым, а живой шард выглядел как
    // сломанный скрипт.
    //
    // Два ограничителя, оба намеренно грубые:
    //
    //   — не больше PostsPerWindow сообщений за WindowLength. Лишние молча выбрасываются:
    //     это хроника, а не журнал событий, терять из неё нечего;
    //   — одинаковые заголовки не повторяются в течение DuplicateWindow. Слухи о смерти
    //     все выглядят как «Слыхали?», и без этого лента состояла бы из них одних.

    private static readonly TimeSpan WindowLength = TimeSpan.FromMinutes(5);
    private const int PostsPerWindow = 12;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);

    private static DateTime _windowStart;
    private static int _postsThisWindow;
    private static readonly Dictionary<string, DateTime> _recentTitles = new();

    private static bool AllowedNow(string title)
    {
        var now = Core.Now;

        if (now - _windowStart >= WindowLength)
        {
            _windowStart = now;
            _postsThisWindow = 0;

            foreach (var stale in new List<string>(_recentTitles.Keys))
            {
                if (now - _recentTitles[stale] >= DuplicateWindow)
                {
                    _recentTitles.Remove(stale);
                }
            }
        }

        if (_postsThisWindow >= PostsPerWindow)
        {
            return false;
        }

        if (title != null)
        {
            if (_recentTitles.TryGetValue(title, out var last) && now - last < DuplicateWindow)
            {
                return false;
            }

            _recentTitles[title] = now;
        }

        _postsThisWindow++;

        return true;
    }

    private sealed class ForumPostBody
    {
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("guild")] public string Guild { get; set; }
        [JsonPropertyName("tag")] public string Tag { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("body")] public string Body { get; set; }
    }

    private static void PostAsync(string author, string guild, string tag, string title, string body)
    {
        if (!IsEnabled || !AllowedNow(title))
        {
            return;
        }

        _ = SendAsync(author, guild, tag, title, body); // fire and forget — errors just get logged
    }

    private static async Task SendAsync(string author, string guild, string tag, string title, string body)
    {
        try
        {
            var payload = new ForumPostBody { Author = author, Guild = guild, Tag = tag, Title = title, Body = body };
            // ConfigureAwait(false): the game loop installs itself as the ambient
            // SynchronizationContext (Main.cs), so a bare await would drag this
            // continuation — and the site's response handling — back onto the main
            // thread for a call that never touches game state.
            var response = await _httpClient.PostAsJsonAsync(_postUrl, payload).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning($"MahaonForumBridge: site returned {(int)response.StatusCode} for post '{title}'.");
            }
        }
        catch (Exception ex)
        {
            // Website down/unreachable — never let this affect the game itself, just log
            // and move on, same as MahaonAiTextGenerator's failure handling.
            _logger.Warning($"MahaonForumBridge: failed to post '{title}' — {ex.Message}");
        }
    }

    private static string GuildNameOf(Mobile bot) => bot?.Guild?.Name;

    private static PersonalityTrait TraitOf(Mobile bot) =>
        BotController.TryGetProfile(bot as PlayerMobile, out var profile) ? profile.Personality.Trait : PersonalityTrait.Balanced;

    // ---- Гильдийные войны ----

    private static readonly string[] WarTemplatesAggressive =
    {
        "Хватит терпеть! {enemy} давно напрашивались — сегодня мы им покажем.",
        "Наконец-то! Пора {enemy} преподать урок раз и навсегда.",
        "Я говорил, что этим кончится. {enemy} сами напросились."
    };

    private static readonly string[] WarTemplatesCautious =
    {
        "Совет решил объявить войну {enemy}. Надеюсь, мы всё просчитали...",
        "Не уверен, что это разумно, но гильдия решила — война с {enemy} началась.",
        "Собираем силы. {enemy} теперь наши враги, будьте осторожны там."
    };

    private static readonly string[] WarTemplatesGreedy =
    {
        "Война с {enemy}! Их добро скоро станет нашим.",
        "{enemy} наконец получат своё — и заодно обеднеют в нашу пользу."
    };

    private static readonly string[] WarTemplatesGenerous =
    {
        "С тяжёлым сердцем сообщаю — гильдия объявила войну {enemy}. Берегите друг друга.",
        "Не хотел этого, но выбора не оставили. {enemy} — теперь враги. Всем держаться вместе."
    };

    private static readonly string[] WarTemplatesBalanced =
    {
        "Официально: наша гильдия в состоянии войны с {enemy}.",
        "Объявление: {enemy} — отныне враги гильдии. Действуйте соответственно."
    };

    /// <summary>Call from DynamicGuildRelations right when a pair's relation flips to
    /// War — one post per side, each from a random member of that guild if one exists
    /// online, flavored by that member's own personality.</summary>
    public static void OnGuildWarDeclared(string guildA, string guildB)
    {
        PostWarSide(guildA, guildB);
        PostWarSide(guildB, guildA);
    }

    private static void PostWarSide(string ownGuild, string enemyGuild)
    {
        var author = PickSpokesbot(ownGuild);
        var trait = author != null ? TraitOf(author) : PersonalityTrait.Balanced;
        var name = author?.Name ?? ownGuild;

        var template = PickTemplate(trait, WarTemplatesAggressive, WarTemplatesCautious, WarTemplatesGreedy, WarTemplatesGenerous, WarTemplatesBalanced);
        var body = template.Replace("{enemy}", enemyGuild);

        PostAsync(name, ownGuild, "war", $"Война с {enemyGuild}!", body);
    }

    // ---- Личные конфликты (испортившиеся отношения) ----

    private static readonly string[] FrictionTemplatesAggressive =
    {
        "{other} меня совсем задолбал(а). В следующий раз спуску не дам.",
        "Ещё раз {other} сунется — пожалеет."
    };

    private static readonly string[] FrictionTemplatesCautious =
    {
        "Стараюсь держаться подальше от {other} теперь. Неприятный тип.",
        "После случившегося с {other} буду осторожнее."
    };

    private static readonly string[] FrictionTemplatesGreedy =
    {
        "{other} мне кое-что задолжал(а). Не забуду.",
        "Из-за {other} я потерял(а) добычу. Это так просто не спущу."
    };

    private static readonly string[] FrictionTemplatesGenerous =
    {
        "Жаль, что так вышло с {other}... надеялся(ась) на лучшее.",
        "Не держу зла на {other}, но осадок остался."
    };

    private static readonly string[] FrictionTemplatesBalanced =
    {
        "Что-то не заладилось у меня с {other} в последнее время.",
        "{other} и я явно не сошлись характерами."
    };

    /// <summary>Call from BotRelationships.Adjust right when a (bot, other) pair's score
    /// crosses INTO hostile territory (wasn't hostile before, is now) — not on every
    /// Adjust call, or this would spam constantly.</summary>
    public static void OnTurnedHostile(Mobile bot, Mobile other)
    {
        if (bot is not PlayerMobile) // only real bots post — a hostile player doesn't need a forum entry written for them
        {
            return;
        }

        var trait = TraitOf(bot);
        var template = PickTemplate(trait, FrictionTemplatesAggressive, FrictionTemplatesCautious, FrictionTemplatesGreedy, FrictionTemplatesGenerous, FrictionTemplatesBalanced);
        var body = template.Replace("{other}", other?.Name ?? "кое-кто");

        PostAsync(bot.Name, GuildNameOf(bot), "friction", "Всё меня раздражает...", body);
    }

    // ---- Слухи ----

    /// <summary>Call from BotRumors.NotableDrop/NotableDeath — the text is already
    /// human-readable there, just needs an author/guild attached.</summary>
    public static void OnRumor(Mobile bot, string rumorText)
    {
        if (bot is not PlayerMobile)
        {
            return;
        }

        PostAsync(bot.Name, GuildNameOf(bot), "rumor", "Слыхали?", rumorText);
    }

    // ---- Городские дела ----

    /// <summary>Call from CityControlSystem.Capture right after a guild takes a city —
    /// posted by a random online member of the capturing guild, same spokesbot pattern
    /// as guild wars.</summary>
    public static void OnCityCaptured(string city, Guild guild)
    {
        var author = PickSpokesbot(guild?.Name);
        var name = author?.Name ?? guild?.Name ?? "Неизвестный";

        var templates = new[]
        {
            $"Город {city} теперь наш! Гильдия {guild?.Name} берёт его под свою руку.",
            $"Свершилось — {city} захвачен гильдией {guild?.Name}. Налог теперь наш.",
            $"{city} пал перед нашей силой. Добро пожаловать в новые владения {guild?.Name}."
        };

        PostAsync(name, guild?.Name, "city_capture", $"{city} захвачен!", templates[Utility.Random(templates.Length)]);
    }

    // ---- Мировые события ----

    /// <summary>Call from MahaonVersaSystem.TriggerRaidNear right when a raid actually
    /// fires — no specific bot "wrote" this, it's a world-event announcement, so author
    /// is a flavor name rather than a real bot.</summary>
    public static void OnRaidTriggered(string city, int raiderCount)
    {
        var templates = new[]
        {
            $"Тревога! На {city} движется отряд из {raiderCount} налётчиков!",
            $"Всем в укрытие — {city} атакован, около {raiderCount} врагов на подходе.",
            $"Дозорные сообщают: {raiderCount} налётчиков у стен {city}. Готовьтесь к бою."
        };

        PostAsync("Дозорный", null, "raid", $"Набег на {city}!", templates[Utility.Random(templates.Length)]);
    }

    // ---- Общие мелочи ----

    private static string PickTemplate(
        PersonalityTrait trait, string[] aggressive, string[] cautious, string[] greedy, string[] generous, string[] balanced
    )
    {
        var pool = trait switch
        {
            PersonalityTrait.Aggressive => aggressive,
            PersonalityTrait.Cautious   => cautious,
            PersonalityTrait.Greedy     => greedy,
            PersonalityTrait.Generous   => generous,
            _                            => balanced
        };

        return pool[Utility.Random(pool.Length)];
    }

    private static Mobile PickSpokesbot(string guildName)
    {
        var guild = BotGuilds.TryGet(guildName);

        if (guild == null || guild.Members.Count == 0)
        {
            return null;
        }

        return guild.Members[Utility.Random(guild.Members.Count)];
    }
}
