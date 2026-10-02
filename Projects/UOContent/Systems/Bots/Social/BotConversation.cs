using System;
using Server.Mobiles;

namespace Server.Systems.Bots;

public enum BotIntent
{
    None,
    Insult,
    Thanks,
    Farewell,
    HowAreYou,
    Who,
    WhatDoing,
    News,
    Home,
    Guild,
    Greeting
}

/// <summary>
/// A bot answering a real player. One bot answers each line: the one named in it, otherwise the
/// nearest within earshot of a casual remark. The meaning is guessed from word stems, and the
/// answer comes from the bot's own state: what it is doing, how it feels, where it lives.
/// </summary>
public static class BotConversation
{
    // Unaddressed remarks are only taken up by a bot standing this close.
    private const int CasualRange = 4;

    private const int AddressedRange = 12;

    // Stems in the order they are tried: an insult beats a greeting in "привет, дурак". A stem
    // ending in '$' must be the whole word, so "пока" does not match "покажи".
    private static readonly (BotIntent intent, string[] stems)[] _stems =
    [
        (BotIntent.Insult, ["дурак", "идиот", "урод", "тупой", "тупица", "козел", "сволоч", "мраз", "дебил", "кретин", "придур"]),
        (BotIntent.Thanks, ["спасиб", "благодар", "пасиб"]),
        (BotIntent.Farewell, ["пока$", "бывай", "до встречи", "до свидан", "прощай", "увидимся"]),
        (BotIntent.HowAreYou, ["как дела", "как жизнь", "как ты", "как поживаешь", "как сам", "как оно"]),
        (BotIntent.Who, ["кто ты", "как тебя зовут", "как зовут", "твое имя", "кем работаешь", "профессия", "чем занимаешься"]),
        (BotIntent.WhatDoing, ["что делаешь", "чем занят", "куда идешь", "куда собрался", "что тут делаешь", "куда путь"]),
        (BotIntent.News, ["что нового", "новост", "что слышно", "слухи", "слыхал"]),
        (BotIntent.Home, ["где живешь", "где ты живешь", "где твой дом", "откуда ты", "где дом"]),
        (BotIntent.Guild, ["гильди"]),
        (BotIntent.Greeting, ["привет", "здравств", "здоров", "хай$", "добрый", "доброе", "доброго", "приветств", "салют", "hello$", "hi$"])
    ];

    // DoSpeech hands the same args to every listener, nearest first; the first bot to see a line
    // decides who answers it.
    private static SpeechEventArgs _lastLine;
    private static Mobile _lastResponder;

    public static bool Listens(Mobile bot, Mobile from) =>
        from is PlayerMobile { NetState: not null } && from != bot && from.Alive && bot.Alive && BotSystem.IsActive(bot);

    public static void Hear(BotMobile bot, SpeechEventArgs e)
    {
        if (!ReferenceEquals(e, _lastLine))
        {
            _lastLine = e;
            _lastResponder = PickResponder(e);
        }

        if (_lastResponder != bot || e.Handled)
        {
            return;
        }

        e.Handled = true;
        var from = e.Mobile;
        var text = Normalize(e.Speech);
        var intent = Classify(text);

        bot.Direction = bot.GetDirectionTo(from);

        // A short pause reads as the bot taking the words in rather than answering instantly.
        Timer.StartTimer(TimeSpan.FromMilliseconds(Utility.RandomMinMax(700, 1600)), () => Answer(bot, from, intent));
    }

    private static Mobile PickResponder(SpeechEventArgs e)
    {
        var from = e.Mobile;
        var map = from.Map;
        if (map == null)
        {
            return null;
        }

        var text = Normalize(e.Speech);
        var casual = Classify(text) != BotIntent.None;

        BotMobile nearest = null;
        var nearestDistance = int.MaxValue;

        foreach (var bot in map.GetMobilesInRange<BotMobile>(from.Location, AddressedRange))
        {
            if (!Listens(bot, from) || bot.Combatant != null)
            {
                continue;
            }

            if (IsNamedIn(bot, text))
            {
                return bot;
            }

            var distance = (int)from.GetDistanceToSqrt(bot);
            if (casual && distance <= CasualRange && distance < nearestDistance)
            {
                nearest = bot;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    public static string Normalize(string speech) => speech.ToLowerInvariant().Replace('ё', 'е');

    // The first word of a bot's name is what a player calls it by.
    private static bool IsNamedIn(Mobile bot, string text)
    {
        var name = bot.Name;
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var space = name.IndexOf(' ');
        var first = Normalize(space > 0 ? name[..space] : name);
        return first.Length >= 3 && ContainsWord(text, first);
    }

    public static BotIntent Classify(string text)
    {
        foreach (var (intent, stems) in _stems)
        {
            foreach (var stem in stems)
            {
                if (ContainsWord(text, stem))
                {
                    return intent;
                }
            }
        }

        return BotIntent.None;
    }

    // The stem has to start a word, so "дурак" does not match "придурака" by accident of position.
    private static bool ContainsWord(string text, string stem)
    {
        var whole = stem[^1] == '$';
        if (whole)
        {
            stem = stem[..^1];
        }

        var index = text.IndexOf(stem, StringComparison.Ordinal);
        while (index >= 0)
        {
            var end = index + stem.Length;
            if ((index == 0 || !char.IsLetter(text[index - 1])) && (!whole || end == text.Length || !char.IsLetter(text[end])))
            {
                return true;
            }

            index = text.IndexOf(stem, index + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static void Answer(BotMobile bot, Mobile from, BotIntent intent)
    {
        if (bot.Deleted || !bot.Alive || from.Deleted || bot.Map != from.Map || !bot.InRange(from, AddressedRange + 2) ||
            bot.GetBrain() is not { } brain)
        {
            return;
        }

        var line = Reply(brain, from, intent);
        if (line == null)
        {
            return;
        }

        BotSpeech.Reply(bot, line);

        if (intent == BotIntent.Insult)
        {
            MahaonBots.BotRelationships.OnSpokenTo(bot, from, -1.0);
            return;
        }

        MahaonBots.BotRelationships.OnSpokenTo(bot, from, 0.1);
        brain.OnSocialized(0.15);
    }

    public static string Reply(BotBrain brain, Mobile from, BotIntent intent)
    {
        var bot = brain.Bot;
        var name = from.Name ?? "дружище";
        var hostile = MahaonBots.BotRelationships.IsHostileTo(bot, from) || brain.HoldsGrudge(from);
        var friendly = MahaonBots.BotRelationships.IsFriendlyTo(bot, from);

        if (hostile && intent is not BotIntent.Insult)
        {
            return Pick("Отстань.", "Не о чем мне с тобой говорить.", "Иди своей дорогой.", "Чего тебе надо?");
        }

        return intent switch
        {
            BotIntent.Insult when bot is BotMobile { IsPk: true } || BotSocialRules.IsOutlaw(bot) =>
                Pick($"Повтори это, {name}, и тебе конец.", "Язык отрежу.", "Договоришься сейчас."),
            BotIntent.Insult => Pick("Сам такой.", "Следи за языком.", "Ну и грубиян.", $"Ты чего, {name}?"),
            BotIntent.Thanks => Pick("Не за что.", "Обращайся.", $"Всегда пожалуйста, {name}."),
            BotIntent.Farewell => Pick($"Бывай, {name}.", "Удачи в пути.", "Ещё увидимся.", "Береги себя."),
            BotIntent.HowAreYou => HowAreYou(brain, friendly),
            BotIntent.Who => Who(bot),
            BotIntent.WhatDoing => Doing(brain.Goal),
            BotIntent.News => News(brain),
            BotIntent.Home => Home(brain),
            BotIntent.Guild => bot.Guild is Guilds.Guild guild
                ? $"Я в гильдии «{guild.Name}». Хорошие ребята."
                : "Я сам по себе, без гильдии.",
            BotIntent.Greeting when friendly => Pick($"О, {name}! Рад тебя видеть.", $"{name}, друг мой! Здравствуй.", $"Здорово, {name}! Как сам?"),
            BotIntent.Greeting => Pick($"Привет, {name}.", $"Здравствуй, {name}.", "Доброго дня.", $"Здорово, {name}."),
            _ => Pick("А?", "Не понял тебя.", "Чего?", "Ты это мне?")
        };
    }

    private static string HowAreYou(BotBrain brain, bool friendly)
    {
        var bot = brain.Bot;

        if (bot.Hits < bot.HitsMax / 2)
        {
            return "Потрёпан, но жив.";
        }

        if (brain.Fatigue > 0.7)
        {
            return "Устал как собака, отдохнуть бы.";
        }

        if (brain.Loneliness > 0.7)
        {
            return "Скучно было одному. Хорошо, что ты подошёл.";
        }

        if ((bot.Backpack?.GetAmount(typeof(Items.Gold)) ?? 0) + Banker.GetBalance(bot) > 100_000)
        {
            return "Грех жаловаться, дела идут.";
        }

        return friendly ? Pick("Отлично, раз тебя встретил!", "Да всё хорошо, а ты как?") : Pick("Нормально.", "Потихоньку.", "Живу помаленьку.");
    }

    private static string Who(PlayerMobile bot)
    {
        var name = bot.Name;
        var space = name?.IndexOf(' ') ?? -1;
        var first = space > 0 ? name[..space] : name;

        return string.IsNullOrEmpty(bot.Title) ? $"Я {first}." : $"Я {first}, {bot.Title}.";
    }

    public static string Doing(BotGoal goal) => goal switch
    {
        DefendTownGoal       => "Город защищаю, набег же!",
        BountyHuntGoal       => "За головой преступника иду, награда хорошая.",
        GuildOrderGoal       => "Заказ в гильдию ремесленников несу.",
        ShelterGoal          => "Прячусь, набег же! И тебе советую.",
        PkGoal               => "Не твоё дело.",
        CraftGoal            => "Ремеслом занят, заказов полно.",
        GatherGoal g         => g.Name switch
        {
            "Горное дело"   => "Руду копаю.",
            "Лесозаготовка" => "Лес рублю.",
            _               => "Рыбу ловлю."
        },
        HuntGoal             => "На охоту собрался.",
        LeadGroupGoal        => "Отряд собираю. Пойдёшь со мной?",
        FollowGroupGoal      => "С отрядом иду.",
        CorpseRunGoal        => "За своим добром иду, помер я недавно.",
        TravelGoal           => "В другой город собрался.",
        TradeGoal            => "Товар продаю.",
        SupplyGoal           => "За припасами иду.",
        BankGoal             => "В банк иду.",
        RestGoal             => "Отдыхаю, ноги гудят.",
        SocializeGoal        => "Поболтать с кем-нибудь хотел.",
        LearnGoal            => "Чертежи новые ищу, учиться хочу.",
        TameGoal             => "Зверя приручить хочу.",
        StableGoal           => "В конюшню иду.",
        TitheGoal            => "В храм, пожертвовать.",
        OutfitGoal           => "Снаряжение подбираю.",
        FarmGoal             => "Урожай собираю.",
        SowGoal              => "Сеять иду.",
        WeaveGoal            => "Пряжу пряду да ткань тку.",
        BuildHouseGoal       => "Место под дом ищу.",
        FurnishGoal          => "Дом обставляю.",
        BuyCityHomeGoal      => "Квартиру в городе присматриваю.",
        FurnishCityHomeGoal  => "Квартиру обставляю.",
        _                    => "Да так, гуляю."
    };

    private static string News(BotBrain brain)
    {
        if (MahaonBots.BotRumors.TryPick(out var rumor))
        {
            return rumor;
        }

        return brain.LastNews ?? "Ничего особенного не слышал.";
    }

    private static string Home(BotBrain brain)
    {
        var bot = brain.Bot;

        if (BotHousing.HomeOf(bot) is { Map: { } houseMap } house)
        {
            return WorldCatalog.FindNearest(houseMap, house.Location) is { } town
                ? $"Дом у меня неподалёку от {town.Name}."
                : "Дом у меня в глуши, подальше от городов.";
        }

        if (BotCityHomes.CityHome(bot) is { Map: { } cityMap } apartment &&
            WorldCatalog.FindNearest(cityMap, apartment.Location) is { } city)
        {
            return $"Квартира у меня в {city.Name}.";
        }

        return brain.HomeCity is { Length: > 0 } homeCity ? $"Я из {homeCity}. Своего угла пока нет." : "Своего угла пока нет.";
    }

    private static string Pick(params string[] lines) => lines[Utility.Random(lines.Length)];
}
