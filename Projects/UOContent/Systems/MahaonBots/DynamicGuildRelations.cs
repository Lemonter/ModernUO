using System;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Кто с кем воюет и кто с кем в союзе.
///
///     Раньше это был чистый бросок кубика: раз в два часа каждая пара гильдий с
///     небольшой вероятностью перескакивала в войну, в союз или обратно в нейтралитет.
///     Политика существовала сама по себе и никак не была связана с тем, что боты делали
///     друг с другом, — гильдии могли годами резать друг друга и оставаться нейтральными,
///     а объявить войну могли те, кто ни разу не встречался.
///
///     Теперь войны и союзы растут из отношений. BotRelationships копит попарные счёта
///     («этот меня бил», «этот меня лечил»), а здесь они складываются по парам гильдий:
///     накопленная вражда между бойцами двух гильдий рано или поздно становится войной, а
///     накопленная взаимопомощь — союзом. Отношения при этом сохраняются на диск, так что
///     политика складывается неделями, а не начинается заново после каждого перезапуска.
///
///     Бросок кубика остался ровно для пар, между которыми вообще ничего не было: без
///     него гильдии, чьи бойцы ни разу не пересеклись, застыли бы в нейтралитете навсегда.
///     Ручная установка отношений из гампа маяка работает как работала — она просто
///     перезаписывает текущее состояние, а дальше оно снова живёт своей жизнью.
/// </summary>
public static class DynamicGuildRelations
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(2);

    // Neutral guilds that change their stance fall out more often than they make friends.
    private const double GuildTurnsToWarChance = 0.6;

    /// <summary>Вероятность сдвига для пары гильдий, между бойцами которых не было вообще
    /// ничего. Только для таких пар — там, где история есть, решает она.</summary>
    private const double ShiftChancePerPair = 0.08;

    /// <summary>Насколько плохо в среднем должны относиться друг к другу бойцы двух
    /// гильдий, чтобы это стало войной. Единица счёта — примерно два удара по боту.</summary>
    private const double WarSentiment = -1.5;

    /// <summary>...и насколько хорошо, чтобы стало союзом.</summary>
    private const double AllySentiment = 1.5;

    /// <summary>Пока счёт держится в этой полосе вокруг нуля, война или союз остывают
    /// обратно в нейтралитет. Полоса уже порогов объявления, чтобы отношения не
    /// перещёлкивались туда-сюда у самой границы.</summary>
    private const double CooldownSentiment = 0.5;

    /// <summary>Сколько пар бойцов должны иметь общую историю, чтобы по ней вообще судить
    /// о гильдиях. Одна случайная стычка — не повод для войны между домами.</summary>
    private const int MinPairsForPolitics = 3;

    private static Timer _pollTimer;

    public static void Initialize()
    {
        _pollTimer = Timer.DelayCall(PollInterval, PollInterval, Poll);
    }

    private static void Poll()
    {
        if (Network.NetState.Instances.Count == 0)
        {
            return; // world "paused" — no drift while no one's online to see it
        }

        var names = BotGuilds.NamePool;

        for (var i = 0; i < names.Length; i++)
        {
            for (var j = i + 1; j < names.Length; j++)
            {
                var current = BotGuilds.GetRelation(names[i], names[j]);
                var (sentiment, pairs) = BotRelationships.GetGuildSentiment(names[i], names[j]);

                BotGuildRelation next;

                if (pairs >= MinPairsForPolitics)
                {
                    next = FromSentiment(current, sentiment);
                }
                else if (Utility.RandomDouble() < ShiftChancePerPair)
                {
                    next = ShiftFrom(current); // истории нет — пусть хоть кубик двигает
                }
                else
                {
                    continue;
                }

                if (next == current)
                {
                    continue;
                }

                BotGuilds.SetRelation(names[i], names[j], next);

                if (next == BotGuildRelation.War)
                {
                    Server.Systems.MahaonAi.MahaonForumBridge.OnGuildWarDeclared(names[i], names[j]);
                }
            }
        }
    }

    /// <summary>
    ///     Во что превращается накопленное отношение между бойцами двух гильдий.
    ///
    ///     Объявить войну или союз можно из любого состояния — вражда, доросшая до порога,
    ///     разрывает и союз тоже. А вот остывание идёт только через нейтралитет: из войны
    ///     сразу в союз не прыгают.
    /// </summary>
    private static BotGuildRelation FromSentiment(BotGuildRelation current, double sentiment)
    {
        if (sentiment <= WarSentiment)
        {
            return BotGuildRelation.War;
        }

        if (sentiment >= AllySentiment)
        {
            return BotGuildRelation.Ally;
        }

        return System.Math.Abs(sentiment) <= CooldownSentiment ? BotGuildRelation.Neutral : current;
    }

    private static BotGuildRelation ShiftFrom(BotGuildRelation current) => current switch
    {
        // Neutral tips toward War slightly more often than Ally — a bit more chaos than
        // peace, reads as a livelier world than a 50/50 coin flip would.
        BotGuildRelation.Neutral => Utility.RandomDouble() < GuildTurnsToWarChance
            ? BotGuildRelation.War
            : BotGuildRelation.Ally,
        // War and Ally both cool back down to Neutral eventually rather than flipping
        // straight to the opposite extreme.
        BotGuildRelation.War  => BotGuildRelation.Neutral,
        BotGuildRelation.Ally => BotGuildRelation.Neutral,
        _                     => current
    };
}
