using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonCities;

/// <summary>
/// Guards of a city passing the time on duty: one says something, a comrade standing near answers.
/// What they talk about is what happened in their city — a thief caught, wages paid or held back,
/// new armour, a hard new teacher, a raid, a claim beaten off, a new holder — or, failing news,
/// the small talk of any garrison and the rumours going round. A city's guard talks now and then,
/// not all the time.
/// </summary>
public static class CityGuardChatter
{
    private enum Topic
    {
        Thief,
        Paid,
        Unpaid,
        NewArmour,
        Teacher,
        Raid,
        ClaimRepelled,
        NewHolder
    }

    private readonly record struct Happening(Topic Topic, string Arg, long At);

    // Long enough to be talked about the next day, not for ever.
    private const long NewsLifetimeMs = 48 * 60 * 60_000L;
    private const long YesterdayMs = 12 * 60 * 60_000L;
    private const int MaxNews = 8;

    private const int PartnerRange = 6;
    private static readonly TimeSpan ReplyDelay = TimeSpan.FromSeconds(3);

    private static readonly Dictionary<string, List<Happening>> _news = new();

    // Lets a test hear the talk: a guard's speech goes to clients only.
    internal static Action<Mobile, string> Said;
    private static readonly Dictionary<string, long> _nextTalk = new();

    private static readonly (string opener, string[] replies)[] SmallTalk =
    [
        ("Поскорей бы получка.", ["И не говори, в кармане ветер гуляет.", "Получка-то будет, а вот хватит ли..."]),
        ("Ноги гудят, весь день на посту.", ["Терпи, сменщик скоро придёт.", "А ты думал, служба — это мёд?"]),
        ("Слыхал, в таверне новое пиво завезли?", ["После смены проверим.", "Капитану только не говори."]),
        ("Опять ночью дежурить.", ["Зато тихо. Пока тихо.", "Ночью хоть не жарко."]),
        ("Говорят, в подземельях опять неспокойно.", ["Лишь бы к нам не лезли.", "Нам-то что, мы город стережём."]),
        ("Капитан сегодня злой как чёрт.", ["Он всегда злой. Работа такая.", "Значит, опять кто-то проспал смену."]),
        ("Жена ворчит, что меня дома не бывает.", ["Скажи ей — город охраняешь, не абы что.", "Моя тоже. Привыкнут."]),
        ("Доспех натёр плечо, сил нет.", ["Подложи тряпицу, все так делают.", "К кузнецу сходи, подогнёт."]),
        ("Эх, сейчас бы жаркого с луком.", ["Не трави душу, до обеда ещё далеко.", "После смены угощаю."])
    ];

    // ---- What happened -----------------------------------------------------------------------

    public static void OnCaught(string city, string name) => Remember(city, Topic.Thief, name);

    public static void OnWages(string city, bool paid) => Remember(city, paid ? Topic.Paid : Topic.Unpaid, null);

    public static void OnNewArmour(string city, string metal) => Remember(city, Topic.NewArmour, metal);

    public static void OnTeacher(string city) => Remember(city, Topic.Teacher, null);

    public static void OnRaid(string city) => Remember(city, Topic.Raid, null);

    public static void OnClaimRepelled(string city, string guild) => Remember(city, Topic.ClaimRepelled, guild);

    public static void OnNewHolder(string city, string guild)
    {
        // A new holder brings a new guard: the old one's talk goes with it.
        _news.Remove(city ?? "");
        Remember(city, Topic.NewHolder, guild);
    }

    private static void Remember(string city, Topic topic, string arg)
    {
        if (city == null)
        {
            return;
        }

        if (!_news.TryGetValue(city, out var news))
        {
            _news[city] = news = [];
        }

        // The latest wage news replaces the earlier: paid and unpaid don't both hold.
        news.RemoveAll(h => h.Topic == topic || topic is Topic.Paid or Topic.Unpaid && h.Topic is Topic.Paid or Topic.Unpaid);
        news.Add(new Happening(topic, arg, Core.TickCount));
        if (news.Count > MaxNews)
        {
            news.RemoveAt(0);
        }
    }

    // ---- Talking -----------------------------------------------------------------------------

    /// <summary>
    /// A guard at ease may start a talk with a comrade near it, when its city's guard hasn't talked
    /// for a while. True when it did.
    /// </summary>
    public static bool TryStart(CityGuard guard)
    {
        var city = guard.City;
        var now = Core.TickCount;
        if (city == null || guard.Combatant != null || guard.Map == null ||
            _nextTalk.TryGetValue(city, out var next) && now - next < 0 || FindPartner(guard) is not { } partner)
        {
            return false;
        }

        _nextTalk[city] = now + Utility.RandomMinMax(4, 8) * 60_000L;

        var (opener, reply) = Pick(city, now);
        guard.Direction = guard.GetDirectionTo(partner);
        guard.Say(opener);
        Said?.Invoke(guard, opener);

        Timer.StartTimer(
            ReplyDelay,
            () =>
            {
                if (!partner.Deleted && partner.Alive && partner.Combatant == null && partner.InRange(guard, PartnerRange + 2))
                {
                    partner.Direction = partner.GetDirectionTo(guard);
                    partner.Say(reply);
                    Said?.Invoke(partner, reply);
                }
            }
        );

        return true;
    }

    private static CityGuard FindPartner(CityGuard guard)
    {
        foreach (var other in guard.Map.GetMobilesInRange<CityGuard>(guard.Location, PartnerRange))
        {
            if (other != guard && other.City == guard.City && other.Alive && other.Combatant == null)
            {
                return other;
            }
        }

        return null;
    }

    // News first, when there is any; otherwise a rumour now and then, or garrison small talk.
    private static (string opener, string reply) Pick(string city, long now)
    {
        if (_news.TryGetValue(city, out var news))
        {
            news.RemoveAll(h => now - h.At >= NewsLifetimeMs);
            if (news.Count > 0 && Utility.RandomDouble() < 0.6)
            {
                return Say(news[Utility.Random(news.Count)], now);
            }
        }

        if (Utility.RandomDouble() < 0.2 && MahaonBots.BotRumors.TryPick(out var rumor))
        {
            return (rumor, Any("Да ну?", "Вот это новость.", "Брешут, поди.", "Слыхал уже."));
        }

        var (opener, replies) = SmallTalk[Utility.Random(SmallTalk.Length)];
        return (opener, replies[Utility.Random(replies.Length)]);
    }

    private static (string opener, string reply) Say(Happening news, long now)
    {
        var when = now - news.At < YesterdayMs ? "сегодня" : "вчера";
        return news.Topic switch
        {
            Topic.Thief => (
                $"Слыхал, {when} поймали воришку, {news.Arg}?",
                Any("Слыхал. Будет знать, как в нашем городе шалить.", "Туда ему и дорога.", "Ловкий был, да не ловчее нас.")
            ),
            Topic.Paid => (
                Any("Получку дали, жить можно!", "Жалованье выдали, вечером гуляем."),
                Any("Надолго ли хватит...", "Вот это дело.", "Первым делом — долг трактирщику.")
            ),
            Topic.Unpaid => (
                Any("Жалованье опять задерживают.", "Без получки сидим, слыхал?"),
                Any("Ещё раз задержат — уйду, честное слово.", "Гильдия, видать, поиздержалась.", "Вот тебе и служба.")
            ),
            Topic.NewArmour => (
                $"Видал, нам доспехи новые выдали — {news.Arg}!",
                Any("Сидят как влитые. Гильдия не скупится.", "Тяжёлые, зато надёжные.", "Блестят — хоть глаза щурь.")
            ),
            Topic.Teacher => (
                Any("Новый наставник гоняет нас до седьмого пота.", "Наставник этот — зверь, а не человек."),
                Any("Зато в бою спасибо скажем.", "Терпи, наука даром не даётся.")
            ),
            Topic.Raid => (
                Any("Набег отбили, а у меня до сих пор руки трясутся.", "Слыхал, разбойники опять к городу подходили?"),
                Any("Ничего, в следующий раз сами их встретим.", "Пусть сунутся ещё — встретим.")
            ),
            Topic.ClaimRepelled => (
                $"Слыхал, {news.Arg} пыталась город у нас отжать?",
                Any("Пусть попробуют ещё раз.", "Отстояли камень, и отстоим снова.")
            ),
            _ => (
                $"Теперь город держит {news.Arg}, свои порядки заводят.",
                Any("Лишь бы платили вовремя.", "Нам что, служба та же.")
            )
        };
    }

    private static string Any(params string[] lines) => lines[Utility.Random(lines.Length)];
}
