using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Aggregate "how do people in general feel about this bot" — separate from
///     BotRelationships (which is PAIRWISE, "how does bot X feel about mobile Y"). This is
///     a single running score per bot, nudged whenever BotRelationships records an
///     attack/help directed at them, checked periodically to set a visible title via
///     NameMod.
///
///     Сохраняется вместе с попарными отношениями (BotRelationships): звание «Уважаемый»
///     или «Ненавистный» зарабатывается неделями, и терять его на каждом перезапуске
///     бессмысленно.
/// </summary>
public sealed class BotReputation : GenericPersistence
{
    private static BotReputation _instance;

    public BotReputation() : base("MahaonBotReputation", 1)
    {
    }

    public static void Configure() => _instance = new BotReputation();

    private const double HighThreshold = 6.0;
    private const double LowThreshold = -6.0;

    private static readonly Dictionary<Mobile, double> Score = new();
    private static readonly Dictionary<Mobile, string> AppliedTitle = new();

    public static void OnAttacked(Mobile bot) => Adjust(bot, -1.0);
    public static void OnHelped(Mobile bot) => Adjust(bot, 0.6);

    private static void Adjust(Mobile bot, double delta)
    {
        // Репутация — про людей, а не про монстров. Сюда приходит противник бота, а им в
        // девяти случаях из десяти оказывается зверь: без этой проверки по подземельям
        // ходили бы «Ненавистные драконы» — NameMod ниже переписывает имя кому угодно.
        if (bot is not PlayerMobile { Deleted: false })
        {
            return;
        }

        Score.TryGetValue(bot, out var current);
        Score[bot] = System.Math.Clamp(current + delta, LowThreshold * 1.5, HighThreshold * 1.5);
        RefreshTitle(bot);
    }

    private static void RefreshTitle(Mobile bot)
    {
        var score = Score.GetValueOrDefault(bot);

        var desiredTitle = score switch
        {
            >= HighThreshold => "Уважаемый",
            <= LowThreshold  => "Ненавистный",
            _                => null
        };

        if (AppliedTitle.TryGetValue(bot, out var current) && current == desiredTitle)
        {
            return; // no change, don't touch NameMod for nothing
        }

        bot.NameMod = desiredTitle == null ? null : $"{desiredTitle} {bot.RawName}";
        AppliedTitle[bot] = desiredTitle;
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var live = 0;

        foreach (var (bot, _) in Score)
        {
            if (bot?.Deleted == false)
            {
                live++;
            }
        }

        writer.WriteEncodedInt(live);

        foreach (var (bot, score) in Score)
        {
            if (bot?.Deleted == false)
            {
                writer.Write(bot);
                writer.Write(score);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var bot = reader.ReadEntity<Mobile>();
            var score = reader.ReadDouble();

            if (bot != null)
            {
                Score[bot] = score;
                RefreshTitle(bot);
            }
        }
    }
}
