using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Попарные отношения: что бот думает о конкретном другом существе.
///
///     Счёт на пару (bot, other) — отрицательный копится от полученных ударов,
///     положительный от помощи. Пара порогов превращает его в два вопроса, которые и
///     задаёт остальной код: «этого я ненавижу?» и «этот мне друг?» (BotSocialRules.IsFriend —
///     друга бот защищает и не трогает сам). Счёт копят рефлексы боя
///     (Systems/Bots/Combat/BotCombat.Blame) и лечение союзников (BotHealing.TryHealAlly).
///
///     Отсюда же растёт политика гильдий: GetGuildSentiment складывает попарные счёта
///     между членами двух гильдий, и DynamicGuildRelations по этой сумме объявляет войну
///     или заключает союз. То есть война между гильдиями — не бросок кубика, а итог того,
///     что их бойцы делали друг с другом.
///
///     Хранится на диске, в отличие от остального состояния ботов: вражда, накопленная за
///     неделю, не должна исчезать от перезапуска — иначе гильдейская политика каждый раз
///     начиналась бы с чистого листа и никуда не приходила.
/// </summary>
public sealed class BotRelationships : GenericPersistence
{
    private static BotRelationships _instance;

    private const double HostileThreshold = -3.0;
    private const double FriendlyThreshold = 3.0;

    /// <summary>Дальше порогов счёт не уходит: иначе одна затяжная драка навсегда
    /// закрепляла бы вражду, из которой уже не выбраться.</summary>
    private const double Limit = 10.0;

    private static readonly Dictionary<(Mobile bot, Mobile other), double> Scores = new();

    public BotRelationships() : base("MahaonBotRelationships", 1)
    {
    }

    public static void Configure() => _instance = new BotRelationships();

    public static double GetScore(Mobile bot, Mobile other) =>
        bot != null && other != null && Scores.TryGetValue((bot, other), out var score) ? score : 0.0;

    public static bool IsHostileTo(Mobile bot, Mobile other) => GetScore(bot, other) <= HostileThreshold;

    public static bool IsFriendlyTo(Mobile bot, Mobile other) => GetScore(bot, other) >= FriendlyThreshold;

    /// <summary>Удар по боту: он запоминает, кто это сделал, и общая репутация бьющего в
    /// мире тоже слегка проседает.</summary>
    public static void OnAttacked(Mobile bot, Mobile other)
    {
        Adjust(bot, other, -0.5);
        BotReputation.OnAttacked(other);
    }

    /// <summary>Помощь боту — лечение, благословение, поднятие после смерти. Кладётся
    /// сразу в обе стороны: тот, кого лечили, помнит добро, а лечивший — что этот ему не
    /// чужой. Без взаимности союзы не складывались бы вовсе, потому что помощь всегда
    /// одностороння.</summary>
    public static void OnHelped(Mobile bot, Mobile other)
    {
        Adjust(bot, other, 0.6);
        Adjust(other, bot, 0.4);
        BotReputation.OnHelped(other);
    }

    private static void Adjust(Mobile bot, Mobile other, double delta)
    {
        if (bot == null || other == null || bot == other)
        {
            return;
        }

        var before = GetScore(bot, other);
        var after = System.Math.Clamp(before + delta, -Limit, Limit);

        Scores[(bot, other)] = after;

        // Запись на форум — только в момент перехода через порог, а не на каждое
        // изменение счёта: иначе одна долгая драка писала бы десяток одинаковых заметок.
        if (before > HostileThreshold && after <= HostileThreshold)
        {
            MahaonAi.MahaonForumBridge.OnTurnedHostile(bot, other);
        }
    }

    /// <summary>
    ///     Как две гильдии в среднем относятся друг к другу — средний попарный счёт по
    ///     всем парам «член одной, член другой», у которых вообще что-то было. Пары без
    ///     истории в среднее не входят: иначе одна настоящая вражда растворилась бы в
    ///     сотне нулей и политика никогда бы не сдвинулась.
    /// </summary>
    public static (double average, int pairs) GetGuildSentiment(string guildA, string guildB)
    {
        if (guildA == null || guildB == null || guildA == guildB)
        {
            return (0.0, 0);
        }

        var total = 0.0;
        var pairs = 0;

        foreach (var ((bot, other), score) in Scores)
        {
            if (score == 0.0 || bot?.Deleted != false || other?.Deleted != false)
            {
                continue;
            }

            var from = bot.Guild?.Name;
            var to = other.Guild?.Name;

            if (from == null || to == null)
            {
                continue;
            }

            if (from == guildA && to == guildB || from == guildB && to == guildA)
            {
                total += score;
                pairs++;
            }
        }

        return pairs == 0 ? (0.0, 0) : (total / pairs, pairs);
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var live = 0;

        foreach (var ((bot, other), _) in Scores)
        {
            if (bot?.Deleted == false && other?.Deleted == false)
            {
                live++;
            }
        }

        writer.WriteEncodedInt(live);

        foreach (var ((bot, other), score) in Scores)
        {
            if (bot?.Deleted != false || other?.Deleted != false)
            {
                continue; // мёртвых пар не сохраняем, иначе таблица растёт вечно
            }

            writer.Write(bot);
            writer.Write(other);
            writer.Write(score);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var bot = reader.ReadEntity<Mobile>();
            var other = reader.ReadEntity<Mobile>();
            var score = reader.ReadDouble();

            if (bot != null && other != null)
            {
                Scores[(bot, other)] = score;
            }
        }
    }
}
