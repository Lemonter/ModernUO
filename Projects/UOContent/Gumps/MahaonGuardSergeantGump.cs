using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonGuard;
using Server.Systems.MahaonRaids;

namespace Server.Gumps;

/// <summary>
///     Сержант Гвидо's own rank is hardcoded to GuardSystem.RankNames[2] ("Сержант") —
///     his dialogue tone is relative to that fixed point: mocks players below it, treats
///     equals as equals, gets more deferential the further above it the player's own real
///     guard rank climbs.
/// </summary>
public class MahaonGuardSergeantGump : DynamicGump
{
    private const int GuidoRank = 2; // Сержант — see GuardSystem.RankNames

    private readonly PlayerMobile _player;
    private readonly Mobile _guido;

    public override bool Singleton => true;

    public MahaonGuardSergeantGump(PlayerMobile player, Mobile guido) : base(50, 50)
    {
        _player = player;
        _guido = guido;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var rank = GuardSystem.GetRank(_player);
        var points = GuardSystem.GetPoints(_player);
        var toNext = GuardSystem.GetPointsToNextRank(_player);
        var hitsBonus = GuardSystem.GetHitsBonus(_player);
        var line = RuGuidoLine(rank);
        var raidLine = RaidStatusLine();
        var questLine = Systems.MahaonQuests.GuardQuestSystem.GetStatusLine(_player, _guido);

        // Высоты строк ниже подобраны под самый длинный вариант каждой: строка про набег
        // доходит до сотни символов («Сейчас идёт набег — на улицах ещё N врагов...»), а
        // стояла в поле высотой в одну строку и обрезалась больше чем наполовину. Окно от
        // этого выросло, зато читается целиком.
        var height = 400;

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);

        builder.AddHtml(15, 15, 290, 20, "Сержант Гвидо");
        // Some RuGuidoLine entries run to ~90 characters (e.g. the rank-10 fallback) — at
        // 290px width that wraps to 3 lines, and the old height=40 (~2 lines) clipped the
        // last line. Everything below shifted down by the same +20 this box grew.
        builder.AddHtml(15, 40, 290, 60, line);

        builder.AddHtml(15, 110, 290, 20, $"Твоё звание: {GuardSystem.RankNames[rank]} ({points} очков)");

        // Mahaon: was never shown anywhere in the gump — GuardSystem already computes it
        // (scaled by how much of a Warrior the player actually is), just wasn't displayed.
        builder.AddHtml(15, 132, 290, 20, $"Бонус к максимальному HP за звание: +{hitsBonus}");

        builder.AddHtml(
            15, 154, 290, 40,
            toNext.HasValue
                ? $"До следующего звания: {toNext.Value} очков."
                : "Ты уже дослужился до максимального звания стражи."
        );

        // Guard points mostly come from real raids (RaidEventSystem.OnCreatureDeath), not
        // this quest board — the quest below is a smaller, steadier top-up alongside it.
        builder.AddHtml(15, 196, 290, 60, raidLine);

        // Mahaon: quest completion used to only show as a one-off chat line at the moment
        // the last required kill landed — re-opening the gump later (or just not reading
        // chat fast enough) showed nothing about it. Now it's always visible here.
        builder.AddHtml(15, 258, 290, 40, questLine);

        builder.AddButton(15, 308, 4005, 4007, 1);
        builder.AddHtml(50, 308, 240, 20, "Задание гвардии");

        builder.AddButton(15, 338, 4005, 4007, 2);
        builder.AddHtml(50, 338, 240, 20, "Награды");

        builder.AddButton(15, 368, 4005, 4007, 0);
        builder.AddHtml(50, 368, 240, 20, "Закрыть");
    }

    private static string RaidStatusLine()
    {
        var active = RaidEventSystem.GetActiveRaiderCount();

        if (active > 0)
        {
            return $"Сейчас идёт набег — на улицах ещё {active} врагов. Бей их — это лучший способ заслужить очки стражи!";
        }

        var remaining = RaidEventSystem.GetTimeUntilNextRaid();

        return remaining <= System.TimeSpan.Zero
            ? "Разведка донесла — набег может начаться в любой момент."
            : $"Следующего набега ждём примерно через {FormatSpan(remaining)}.";
    }

    private static string FormatSpan(System.TimeSpan span) =>
        span.TotalHours >= 1 ? $"{(int)span.TotalHours} ч {span.Minutes} мин" : $"{span.Minutes} мин";

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            Systems.MahaonQuests.GuardQuestSystem.Talk(_player, _guido);
        }
        else if (info.ButtonID == 2)
        {
            Systems.MahaonGuard.GuardRewardShop.Open(_player);
        }
    }

    private static string RuGuidoLine(int playerRank) => playerRank switch
    {
        0 => "Экий ты ещё зелёный, новобранец. Дослужись хотя бы до сержанта — тогда и поговорим как равные.",
        1 => "А, стражник. Растёшь помаленьку. Ещё одна ступенька — и будешь мне ровня.",
        GuidoRank => "О, тоже сержант? Наконец есть с кем поговорить без чинопочитания. Держись, приятель.",
        3 => "Так точно, лейтенант. Чем сержант может быть полезен?",
        4 => "Капитан! Рад видеть вас в добром здравии. Приказывайте.",
        5 => "Господин командир, для меня честь, что вы вообще сюда заглянули.",
        6 => "Маршал?! Простите мою дерзость, что вообще посмел заговорить первым.",
        7 => "Генералиссимус... Я даже не знаю, как положено к вам обращаться. Слушаю.",
        8 => "Легенда стражи собственной персоной. О тебе слагают байки в каждой казарме.",
        9 => "Хранитель города... Пока ты здесь, я сплю спокойно. Приказывай, что угодно.",
        _ => "Живая легенда стоит передо мной. Мне нечего тебе предложить — это ты мог бы меня учить."
    };
}
