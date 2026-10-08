using System.Collections.Generic;
using Server.Systems.MahaonRaids;

namespace Server.Systems.Bots;

/// <summary>
/// A town under attack calls its fighters: a bot that can hold its own goes to where the raiders
/// are and fights them off — the braver, the readier. The caution of the cautious and the
/// weakness of the weak keep them out of it; see <see cref="ShelterGoal"/>.
/// </summary>
public sealed class DefendTownGoal : BotGoal
{
    private const int CallRange = 250;
    private const int MinFightingSkill = 50;

    public override string Name => "Оборона города";

    public override string[] News => ["Отбивали набег, еле отстояли город.", "Видал, сколько их полегло у стен?"];

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotCombatStyles.FightingSkill(bot) < MinFightingSkill || bot.Hits < bot.HitsMax * 0.7 ||
            RaidAlarm.Nearest(bot.Map, bot.Location, CallRange) == null)
        {
            return 0;
        }

        // Above most work: a town on fire beats a full pack of ore.
        return 0.8 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.4;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (RaidAlarm.Nearest(bot.Map, bot.Location, CallRange) is not { } raid)
        {
            return null;
        }

        BotSpeech.SayText(bot, $"Набег на {raid.City}! За мной, защитим город!");
        return [new GoToAction(raid.Map, raid.Location, 8, $"на защиту {raid.City}"), new DefendAction(raid)];
    }
}

/// <summary>Fights the raiders where the raid struck until none is left near.</summary>
public sealed class DefendAction : BotAction
{
    private const long MaxMs = 20 * 60_000;

    private readonly RaidAlert _raid;
    private long _until;

    public DefendAction(RaidAlert raid) => _raid = raid;

    public override void Start(BotBrain brain) => _until = Core.TickCount + MaxMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        if (Core.TickCount - _until >= 0)
        {
            return BotActionResult.Done();
        }

        if (RaidAlarm.RaiderNear(_raid) is not { } raider)
        {
            BotSpeech.SayText(brain.Bot, "Отбились! Город наш.");
            return BotActionResult.Done(2000);
        }

        // The reflexes take it from here; this just keeps pointing them at the raiders.
        if (brain.Combat.Opponent is not { Alive: true })
        {
            BotCombat.Engage(brain, raider);
        }

        return BotActionResult.Running(1000);
    }

    public override string Describe(BotBrain brain) => $"Обороняет {_raid.City}";
}

/// <summary>
/// Those who can't fight get out of a raid's way: home if they have one away from the fighting,
/// otherwise out past the edge of town until it is over.
/// </summary>
public sealed class ShelterGoal : BotGoal
{
    private const int DangerRange = 60;
    private const int SafeDistance = 90;

    public override string Name => "Укрыться от набега";

    public override bool IsUpkeep => true;

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotCombatStyles.FightingSkill(bot) >= 50 && brain.Caution < 70 ||
            RaidAlarm.Nearest(bot.Map, bot.Location, DangerRange) == null)
        {
            return 0;
        }

        return 1.3;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (RaidAlarm.Nearest(bot.Map, bot.Location, DangerRange) is not { } raid)
        {
            return null;
        }

        BotSpeech.SayText(bot, "Набег! Прячьтесь!");

        if (BotHousing.HomeOf(bot) is { Map: { } map } house && map == bot.Map &&
            !Utility.InRange(house.Location, raid.Location, DangerRange))
        {
            return [new GoToAction(map, BotHousing.Inside(house), 1, "домой, переждать")];
        }

        // Straight away from the raid.
        var dx = bot.X - raid.Location.X;
        var dy = bot.Y - raid.Location.Y;
        var len = System.Math.Max(1.0, System.Math.Sqrt(dx * dx + dy * dy));
        var x = raid.Location.X + (int)(dx / len * SafeDistance);
        var y = raid.Location.Y + (int)(dy / len * SafeDistance);
        var z = bot.Map.GetAverageZ(x, y);

        return [new GoToAction(bot.Map, new Point3D(x, y, z), 6, "подальше от набега"), new RestAction(5 * 60_000)];
    }
}
