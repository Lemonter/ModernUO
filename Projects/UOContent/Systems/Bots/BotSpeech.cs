using System.Collections.Generic;

namespace Server.Systems.Bots;

public enum BotTopic
{
    Greeting,
    SmallTalk,
    Farewell,
    Idle,
    Resting,
    Traveling,
    Arrived,
    Banking,
    Dead,
    Resurrected,
    GroupForming,
    GroupDone,
    Threat,
    Victory
}

/// <summary>
/// Template speech. Lines are picked at random per topic; {0} is the listener's name where a line
/// addresses someone. A per-bot cooldown keeps a crowd of bots from turning a town into spam.
/// </summary>
public static class BotSpeech
{
    private const long CooldownMs = 6000;

    private static readonly Dictionary<Mobile, long> _lastSpoke = new();

    private static readonly Dictionary<BotTopic, string[]> _lines = new()
    {
        [BotTopic.Greeting] =
        [
            "Привет, {0}!", "Здорово, {0}.", "О, {0}! Сколько лет, сколько зим.", "Доброго дня, {0}.", "{0}, как жизнь?",
            "Приветствую.", "Хай.", "Йо, {0}!"
        ],
        [BotTopic.SmallTalk] =
        [
            "Слышал, в подземельях опять неспокойно.", "Цены на слитки совсем взбесились.", "Погодка сегодня что надо.",
            "Говорят, у Деспайза видели драконов.", "Надо бы реагентов прикупить.", "Банк сегодня битком.",
            "Ты в гильдии?", "Я вчера чуть не помер от лича.", "Кто-нибудь знает хорошего кузнеца?", "Скукота.",
            "Давно в этих краях?", "Работы много, денег мало.", "Куда путь держишь?", "Опять ПК у Минока шалят.",
            "Мне бы коня получше.", "Ха, точно.", "Да ну?", "Ага.", "Вот и я о том же.", "Не говори."
        ],
        [BotTopic.Farewell] = ["Бывай, {0}.", "Ладно, пойду.", "Увидимся.", "Удачи, {0}!", "Ну, мне пора.", "До встречи."],
        [BotTopic.Idle] = ["Хм.", "Эх...", "Чем бы заняться.", "*зевает*", "*потягивается*", "*оглядывается*"],
        [BotTopic.Resting] = ["Передохну немного.", "Ноги гудят.", "*садится отдохнуть*", "Устал как собака."],
        [BotTopic.Traveling] = ["Пора в путь.", "Схожу-ка я в другой город.", "Засиделся я тут.", "Дорога зовёт."],
        [BotTopic.Arrived] = ["Наконец-то добрался.", "Ну вот я и здесь.", "Фух, дошёл."],
        [BotTopic.Banking] = ["Надо в банк заглянуть.", "Отнесу-ка золото в банк."],
        [BotTopic.Dead] = ["Ооооо...", "ОоОоОо!"],
        [BotTopic.Resurrected] = ["Спасибо, лекарь!", "Снова в строю.", "Жизнь прекрасна."],
        [BotTopic.GroupForming] = ["Кто со мной в подземелье?", "Собираю отряд, пошли!", "Айда на охоту, вместе веселее.", "За мной, народ!"],
        [BotTopic.GroupDone] = ["Хорошо поохотились.", "Расходимся, спасибо всем.", "Неплохой улов, бывайте."],
        [BotTopic.Threat] = ["Ну держись!", "Сейчас получишь!", "Ты пожалеешь!", "Умри!", "Твоя смерть пришла."],
        [BotTopic.Victory] = ["Так тебе и надо.", "Следующий!", "Легко.", "Хе-хе."]
    };

    public static void Say(Mobile bot, BotTopic topic, Mobile target = null, double chance = 1.0)
    {
        if (chance < 1.0 && Utility.RandomDouble() >= chance)
        {
            return;
        }

        var now = Core.TickCount;
        if (_lastSpoke.TryGetValue(bot, out var last) && now - last < CooldownMs)
        {
            return;
        }

        if (!_lines.TryGetValue(topic, out var lines) || lines.Length == 0)
        {
            return;
        }

        _lastSpoke[bot] = now;

        var line = lines[Utility.Random(lines.Length)];
        bot.Say(line.Contains("{0}") ? string.Format(line, target?.Name ?? "дружище") : line);
    }

    /// <summary>Says a ready line, under the same cooldown as the templates.</summary>
    public static bool SayText(Mobile bot, string text)
    {
        var now = Core.TickCount;
        if (string.IsNullOrEmpty(text) || _lastSpoke.TryGetValue(bot, out var last) && now - last < CooldownMs)
        {
            return false;
        }

        _lastSpoke[bot] = now;
        bot.Say(text);
        return true;
    }

    // News older than this is stale; a bot talks about its morning, not last week.
    private const long NewsFreshMs = 2 * 60 * 60_000;

    /// <summary>
    /// A line of conversation: now and then a rumour going round, or what the bot itself has
    /// been up to, otherwise small talk. Rumours are how word of a raid or a dragon kill spreads.
    /// </summary>
    public static void Chat(Mobile bot, Mobile listener)
    {
        var roll = Utility.RandomDouble();

        if (roll < 0.25 && MahaonBots.BotRumors.TryPick(out var rumor) && SayText(bot, rumor))
        {
            return;
        }

        if (roll < 0.5 && bot.GetBrain() is { LastNews: { } news } brain && Core.TickCount - brain.LastNewsTick < NewsFreshMs &&
            SayText(bot, news))
        {
            return;
        }

        Say(bot, BotTopic.SmallTalk, listener);
    }

    public static void Forget(Mobile bot) => _lastSpoke.Remove(bot);
}
