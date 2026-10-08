namespace Server.Systems.MahaonBots;

/// <summary>
///     Generates bot names without trailing numbers. Mixes a generous first-name pool with
///     a surname pool, and occasionally throws in a "joke twin" — a near-duplicate spelling
///     of a name that already exists (like Эрик / Эррик / ЭРРИК) for flavor, the way real
///     shards end up with running-gag alt names.
/// </summary>
public static class BotNameGenerator
{
    private static readonly string[] FirstNames =
    [
        "Эрик", "Богдан", "Тимофей", "Ярослав", "Марк", "Кирилл", "Северин", "Родион",
        "Матвей", "Демьян", "Остап", "Данила", "Прохор", "Фадей", "Григорий", "Аким",
        "Спиридон", "Егор", "Захар", "Тихон", "Влас", "Никодим", "Осип", "Устин",
        "Марьяна", "Агата", "Злата", "Милана", "Северина", "Ярина", "Веста", "Стефания",
        "Ладислава", "Радмила", "Драгана", "Огняна", "Всеслава", "Даромила", "Богумила",
        "Гаврила", "Онуфрий", "Панкрат", "Ерофей", "Лукьян", "Аристарх", "Феофан",
        "Всеволод", "Мирослав", "Ратибор", "Добрыня", "Светозар", "Велимир", "Радим",
        "Ждан", "Путята", "Синеус", "Услад", "Хотен", "Чурила", "Яромил"
    ];

    private static readonly string[] Surnames =
    [
        "Волков", "Ковалёв", "Морозов", "Соколов", "Быков", "Лисицын", "Медведев",
        "Зайцев", "Воронов", "Орлов", "Ершов", "Гусев", "Дроздов", "Журавлёв",
        "Сорокин", "Куницын", "Барсуков", "Ястребов", "Филин", "Скворцов",
        "Кузнец", "Мельник", "Гончар", "Плотник", "Косарь", "Пастух", "Рыбак",
        "Странник", "Бродяга", "Скиталец", "Отшельник", "Пришлый", "Безымянный",
        "Полынный", "Кремнёвый", "Смолистый", "Дубовый", "Тернистый", "Пепельный"
    ];

    // A handful of names that get "joke twin" spelling variants when picked — real shard
    // running-gag energy: Эрик / Эррик / ЭРРИК all wandering around.
    private static readonly (string original, string[] variants)[] JokeTwins =
    [
        ("Эрик", ["Эррик", "ЭРРИК", "Эриик"]),
        ("Богдан", ["Богдаан", "БОГДАН"]),
        ("Захар", ["Заххар", "ЗАХАР"])
    ];

    private const double JokeTwinChance = 0.08;

    public static string Generate()
    {
        var first = FirstNames.RandomElement();

        if (Utility.RandomDouble() < JokeTwinChance)
        {
            foreach (var (original, variants) in JokeTwins)
            {
                if (first == original)
                {
                    first = variants.RandomElement();
                    break;
                }
            }
        }

        var surname = Surnames.RandomElement();
        return $"{first} {surname}";
    }
}
