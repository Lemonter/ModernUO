namespace Server.Systems.MahaonBots;

/// <summary>
///     Все вероятности поведения ботов в одном месте.
///
///     До этого они стояли числами прямо в коде — три десятка «RandomDouble() &lt; 0.15»,
///     разбросанных по восьми файлам. Читающий не мог сказать, много это или мало, не
///     найдя все остальные и не сравнив; правящий не мог поменять «боты слишком часто
///     воруют», не перечитав весь BotSocial. Здесь у каждого числа есть имя и соседи, с
///     которыми его можно сравнить.
///
///     Числа перенесены как были — это не перебалансировка, а перенос. Менять их теперь
///     можно здесь, по одному, не трогая логику.
/// </summary>
public static class BotTuning
{
    // ---------------------------------------------------------------- кем бот родится
    /// <summary>Доля разбойников среди новых ботов.</summary>
    public const double PkChance = 0.12;

    /// <summary>Доля магов, которые тяготеют к некромантии, а не к обычной магии.</summary>
    public const double MagePrefersNecromancyChance = 0.3;

    // ---------------------------------------------------------------- чем занять себя
    /// <summary>Разбойник почти всегда идёт искать жертву, а не заниматься делами.</summary>
    public const double PkGoesHuntingChance = 0.8;

    /// <summary>Зайти в банк перед тем, как браться за дело.</summary>
    public const double BankingTripChance = 0.2;

    /// <summary>Обзавестись верховым животным, если его ещё нет.</summary>
    public const double AcquireMountChance = 0.05;

    /// <summary>Ночью — просто просидеть её в городе вместо нового дела.</summary>
    public const double NightIdleChance = 0.25;

    /// <summary>Бросить слоняться и вернуться к выбору занятия.</summary>
    public const double StopWanderingChance = 0.4;

    /// <summary>Крепкий боец идёт в подземелье один, не собирая отряд.</summary>
    public const double SoloDungeonChance = 0.5;

    /// <summary>Ремесленник берётся за навык своей профессии, а не за случайный.</summary>
    public const double CraftOwnProfessionChance = 0.7;

    /// <summary>Свернуть с обычного маршрута на город, где сейчас бойко торгуют.</summary>
    public const double HotMarketDetourChance = 0.6;

    // ---------------------------------------------------------------- городские повадки
    /// <summary>Вор пробует обчистить соседа, если рядом нет стражи.</summary>
    public const double StealAttemptChance = 0.15;

    /// <summary>Бард просит подаяния.</summary>
    public const double BegAttemptChance = 0.15;

    /// <summary>Лучник пробует приручить зверя поблизости.</summary>
    public const double TameAttemptChance = 0.1;

    // ---------------------------------------------------------------- что говорят вслух
    /// <summary>Выкрикнуть «Дело плохо...», убегая из боя.</summary>
    public const double PanicShoutChance = 0.3;

    /// <summary>Выкрикнуть «Набег! Бежим!», удирая от рейдовых тварей.</summary>
    public const double RaidFleeShoutChance = 0.3;

    /// <summary>Перекинуться словом, пока отряд собирается на месте встречи.</summary>
    public const double IdleGroupChatChance = 0.08;

    // ---------------------------------------------------------------- бой: не-маги
    /// <summary>Рыцарь залечивает раны Close Wounds.</summary>
    public const double ChivalryHealChance = 0.4;

    /// <summary>Воин пускает в ход приём бусидо.</summary>
    public const double BushidoMoveChance = 0.25;

    /// <summary>Бард наводит Разлад на противника.</summary>
    public const double DiscordanceChance = 0.5;

    // ---------------------------------------------------------------- бой: маги
    /// <summary>Маг вообще что-то колдует на этом тике, а не просто стоит в бою.</summary>
    public const double MageCastsThisTickChance = 0.95;

    /// <summary>Обратиться в боевую форму (превращение).</summary>
    public const double MageTransformChance = 0.15;

    /// <summary>Призвать фамильяра.</summary>
    public const double MageSummonFamiliarChance = 0.1;

    /// <summary>Отгородиться стеной, когда телепортироваться некуда.</summary>
    public const double MageWallChance = 0.6;

    /// <summary>Поставить поле — ядовитое или огненное.</summary>
    public const double MageFieldChance = 0.15;

    /// <summary>Отравить противника.</summary>
    public const double MagePoisonChance = 0.35;

    /// <summary>Сковать противника параличом.</summary>
    public const double MageParalyzeChance = 0.25;

    /// <summary>Навесить проклятие вместо прямого удара.</summary>
    public const double MageDebuffChance = 0.2;

    // ---------------------------------------------------------------- гильдии
    /// <summary>Нейтральные гильдии, меняя отношения, чаще расходятся во вражду, чем
    /// сходятся в союз.</summary>
    public const double GuildTurnsToWarChance = 0.6;
}
