using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.Bots;

/// <summary>What a v2 bot is thinking: needs, character, goal scores, plan. Refresh re-reads.</summary>
public class BotInspectGump : DynamicGump
{
    private const int Width = 420;
    private const int LineHeight = 18;

    private readonly BotMobile _bot;

    public override bool Singleton => true;

    private BotInspectGump(BotMobile bot) : base(60, 60) => _bot = bot;

    public static void DisplayTo(Mobile from, BotMobile bot)
    {
        if (from?.NetState == null || bot?.Brain == null)
        {
            return;
        }

        from.SendGump(new BotInspectGump(bot));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var brain = _bot.Brain;
        var lines = 16 + brain.LastScores.Count + brain.Plan.Count;
        var height = 40 + lines * LineHeight;

        builder.AddPage();
        builder.AddBackground(0, 0, Width, height, 5054);
        builder.AddAlphaRegion(8, 8, Width - 16, height - 16);

        var y = 14;

        builder.AddLabel(16, y, 0x35, $"{_bot.Name} — {(_bot.Alive ? "жив" : "призрак")}, {(brain.Registered ? "v2" : "не активен")}");
        builder.AddButton(Width - 40, y, 0xFA5, 0xFA7, 1); // refresh
        y += LineHeight + 4;

        builder.AddLabel(16, y, 0x481, $"Дом: {brain.HomeCity ?? "—"} {brain.Home} · Сейчас: {_bot.Location} {_bot.Map}");
        y += LineHeight;

        builder.AddLabel(
            16, y, 0x481,
            $"Характер: жадн {brain.Greed} · осторожн {brain.Caution} · общит {brain.Sociability} · бродяга {brain.Wanderlust} · трудолюб {brain.Diligence}"
        );
        y += LineHeight;

        builder.AddLabel(
            16, y, 0x481,
            $"Потребности: усталость {brain.Fatigue:F2} · одиночество {brain.Loneliness:F2} · тяга в путь {brain.Restlessness:F2}"
        );
        y += LineHeight + 6;

        builder.AddLabel(16, y, 0x35, $"Цель: {brain.Goal?.Name ?? "—"}");
        y += LineHeight;

        builder.AddLabel(16, y, 0x481, $"Действие: {brain.DescribeAction()}");
        y += LineHeight;

        builder.AddLabel(16, y, 0x35, "Дальше по плану:");
        y += LineHeight;

        foreach (var step in brain.Plan)
        {
            builder.AddLabelCropped(28, y, Width - 44, LineHeight, 0x481, step.Describe(brain));
            y += LineHeight;
        }

        y += 6;
        builder.AddLabel(16, y, 0x35, "Оценки целей при последнем выборе:");
        y += LineHeight;

        foreach (var (goal, score) in brain.LastScores)
        {
            builder.AddLabel(28, y, goal == brain.Goal ? 0x44 : 0x481, $"{goal.Name}: {score:F2}");
            y += LineHeight;
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1 && !_bot.Deleted)
        {
            DisplayTo(sender.Mobile, _bot);
        }
    }
}
