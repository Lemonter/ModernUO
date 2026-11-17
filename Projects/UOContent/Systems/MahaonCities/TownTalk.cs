using System;
using System.Collections.Generic;
using Server.Misc;

namespace Server.Systems.MahaonCities;

/// <summary>
/// Что горожане обсуждают из случившегося: набеги — начатые, отбитые, с взорванным банком,
/// рассеявшиеся. Свой город обсуждают охотнее и подробнее, о чужом говорят понаслышке. Берут
/// отсюда торговцы за прилавками и боты в разговоре; у стражи свои новости (CityGuardChatter).
/// </summary>
public static class TownTalk
{
    public enum Kind
    {
        RaidStarted,
        RaidRepelled,
        BankBlown,
        RaidGone
    }

    private readonly record struct News(Kind Kind, string City, string Genitive, string Faction, long At);

    // Сутки-двое о набеге ещё говорят, потом забывают.
    private const long LifetimeMs = 48 * 60 * 60_000L;
    private const int MaxNews = 16;

    private static readonly List<News> _news = [];

    // ---- что случилось ----------------------------------------------------------------------

    public static void OnRaid(Kind kind, string city, string genitive, string faction)
    {
        if (city == null)
        {
            return;
        }

        // Исход заменяет весть о начале: «отбили» говорят вместо «напали».
        if (kind != Kind.RaidStarted)
        {
            _news.RemoveAll(n => n.City == city && n.Kind == Kind.RaidStarted);
        }

        _news.Add(new News(kind, city, genitive ?? city, faction, Core.TickCount));
        if (_news.Count > MaxNews)
        {
            _news.RemoveAt(0);
        }
    }

    internal static void Clear() => _news.Clear();

    // ---- разговор ---------------------------------------------------------------------------

    /// <summary>
    /// Пара реплик о набеге для горожан города <paramref name="city"/>: о своём, если было, иначе о
    /// чужом. Эмоция — с которой отвечает собеседник.
    /// </summary>
    public static bool TryRaidTalk(string city, out string opener, out string reply, out Emote? emote)
    {
        opener = reply = null;
        emote = null;

        var now = Core.TickCount;
        _news.RemoveAll(n => now - n.At >= LifetimeMs);
        if (_news.Count == 0)
        {
            return false;
        }

        var own = city == null ? null : Latest(n => string.Equals(n.City, city, StringComparison.OrdinalIgnoreCase));
        (opener, reply, emote) = own is { } mine ? AboutOwn(mine) : AboutOther(_news[Utility.Random(_news.Count)]);
        return opener != null;
    }

    private static News? Latest(Predicate<News> match)
    {
        for (var i = _news.Count - 1; i >= 0; i--)
        {
            if (match(_news[i]))
            {
                return _news[i];
            }
        }

        return null;
    }

    private static (string, string, Emote?) AboutOwn(News n)
    {
        var f = n.Faction;
        return n.Kind switch
        {
            Kind.RaidStarted => Utility.Random(3) switch
            {
                0 => ($"Опять приспешники {f} у наших стен!", Any("Только бы стража выстояла.", "Ставни закрою — и в погреб.", "Сколько можно-то..."), Emote.Sigh),
                1 => ("Слышишь? Опять тревогу трубят.", Any("Набег, не иначе. Товар прячь.", "Лишь бы до лавок не дошли."), Emote.Gasp),
                _ => ($"Говорят, приспешников {f} там тьма-тьмущая.", Any("Брешут. Но лавку я сегодня закрою пораньше.", "Ох, не к добру это."), Emote.Oh)
            },
            Kind.RaidRepelled => Utility.Random(3) switch
            {
                0 => ($"Отбили набег {f}! Слыхал?", Any("Слыхал! Вот это наши дали!", "Ну, за защитников!"), Emote.Cheer),
                1 => ($"Пока приспешники {f} у ворот бились, я весь товар под прилавок попрятал.", Any("Ха! И я так же.", "А я и сам под прилавок залез."), Emote.Laugh),
                _ => ("Стража-то наша, оказывается, не зря жалованье ест.", Any("Вот и я говорю — молодцы!", "Скажу капитану, пусть пиво им выставит."), Emote.Applaud)
            },
            Kind.BankBlown => Utility.Random(2) switch
            {
                0 => ("Банк-то наш взорвали, ироды!", Any("Где теперь деньги держать... в сапоге?", "Всё нажитое — прахом."), Emote.Groan),
                _ => ($"После {f} от банка одни головешки остались.", Any("Отстроят. Не впервой.", "Чтоб им пусто было."), Emote.Sigh)
            },
            _ => ($"Приспешники {f} постояли у стен да ушли.", Any("И слава богам.", "Вернутся ещё, вот увидишь."), Emote.Sigh)
        };
    }

    private static (string, string, Emote?) AboutOther(News n)
    {
        var f = n.Faction;
        var g = n.Genitive;
        return n.Kind switch
        {
            Kind.RaidStarted => ($"Слыхал, у стен {g} приспешники {f}?", Any("Лишь бы к нам не пошли.", "Бедолаги. Цены теперь подскочат."), Emote.Gasp),
            Kind.RaidRepelled => ($"Говорят, защитники {g} отбили {f}.", Any("Крепкий там народ.", "Молодцы, ничего не скажешь."), Emote.Yea),
            Kind.BankBlown => ($"Банк {g} взорвали, слыхал?", Any("Да ну! Вот беда-то.", "Хорошо, что я там денег не держу."), Emote.Gasp),
            _ => ($"Говорят, от {g} приспешники {f} ушли ни с чем.", Any("Ну и славно.", "Видать, зубы обломали."), Emote.Laugh)
        };
    }

    private static string Any(params string[] lines) => lines[Utility.Random(lines.Length)];
}
