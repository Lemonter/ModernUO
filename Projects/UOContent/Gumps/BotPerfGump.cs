using Server.Systems.MahaonBots;

namespace Server.Gumps;

public class BotPerfGump : StaticGump<BotPerfGump>
{
    public override bool Singleton => true;
    protected override bool Cached => false;

    public BotPerfGump() : base(80, 80)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var s = BotPerf.GetSnapshot();

        builder.AddPage();
        builder.AddBackground(0, 0, 420, 260, 5054);
        builder.AddAlphaRegion(10, 10, 400, 240);

        builder.AddHtml(20, 15, 380, 20, $"Bot perf (окно {s.WindowSeconds:F0} сек, живое обновление)");

        var lines = new (string Label, string Value)[]
        {
            ("Ботов сейчас:", s.ActiveBots.ToString()),
            ("Решения — вызовов:", s.ConveyorCalls.ToString()),
            ("Решения — бот-проходов:", s.ConveyorBotsTouched.ToString()),
            ("Решения — мкс/бот:", $"{s.ConveyorUsPerBot:F1}"),
            ("Решения — мс/сек:", $"{s.ConveyorMsPerSec:F2}"),
            ("Движение — вызовов:", s.MovementCalls.ToString()),
            ("Движение — бот-проходов:", s.MovementBotsTouched.ToString()),
            ("Движение — мкс/бот:", $"{s.MovementUsPerBot:F1}"),
            ("Движение — мс/сек:", $"{s.MovementMsPerSec:F2}")
        };

        var y = 45;
        foreach (var (label, value) in lines)
        {
            builder.AddLabel(20, y, 0x480, label);
            builder.AddLabel(220, y, 0x59, value);
            y += 22;
        }

        y += 6;
        builder.AddLabel(20, y, 0x22, $"Итого: ~{s.TotalMsPerSec:F2} мс/сек (~{s.PercentOfOneCore:F2}% одного ядра)");
    }
}
