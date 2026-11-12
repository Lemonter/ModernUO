using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonHerbalism;

namespace Server.Systems.Bots;

/// <summary>
/// Сбор диких трав: уйти от дома в поле или лес, собрать что растёт и по силам, вернуться с
/// пучками. Трава сама по себе растёт только вокруг подключённых игроков, поэтому бот на
/// время похода отмечается у MahaonHerbSpawner собирателем — и находит ровно то, что нашёл
/// бы игрок на его месте: ту же местность, тот же сезон, те же пороги навыка.
/// </summary>
public sealed class HerbForageGoal : BotGoal
{
    // Ниже этого бот не считает себя травником: подорожник собрать сможет, но идти ради
    // него в поле — нет.
    public const double MinSkill = 10.0;

    public override string Name => "Сбор трав";

    public override string[] News =>
    [
        "Набрал полную сумку трав, руки пахнут полынью.", "Нашёл поляну зверобоя — никому не скажу где.",
        "Травы в этом году хорошие.", "Ходил за травами, промочил ноги на болоте."
    ];

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        var skill = bot.Skills[SkillName.Herbalism].Value;

        if (skill < MinSkill)
        {
            return 0;
        }

        var score = 0.15 + BotBrain.Trait(brain.Diligence) * 0.3 + BotBrain.Trait(brain.Greed) * 0.1 + skill / 100.0 * 0.35;
        score -= brain.Fatigue * 0.6;

        // Травнику нужно сырьё для ступки: пустая сумка зовёт в поле сильнее.
        if (BotCrafting.IsHerbalist(bot) && HerbsCarried(bot) < 10)
        {
            score += 0.15;
        }

        if (BotGoods.ValueCarried(bot) > 1500 * BotGoods.TripCapacity(bot))
        {
            score *= 0.3;
        }

        return score;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var center = bot.Map == brain.HomeMap && brain.HomeMap != null ? brain.Home : bot.Location;
        var map = bot.Map;

        if (map == null || !HerbForageAction.TryPickWild(map, center, 15, 70, out var spot))
        {
            return null;
        }

        var quota = 8 + brain.Diligence / 10;
        return [new GoToAction(map, spot, 2, "за травами"), new HerbForageAction(quota)];
    }

    private static int HerbsCarried(Mobile bot)
    {
        var total = 0;
        foreach (var item in bot.Backpack?.Items ?? [])
        {
            if (item is MahaonHerbItem)
            {
                total += item.Amount;
            }
        }

        return total;
    }
}

/// <summary>
/// Собирает кусты вокруг себя, переходя на новое место, когда рядом пусто. Каждый куст —
/// обычный двойной клик, как у игрока: навык растёт, неудачи случаются, семена падают.
/// </summary>
public sealed class HerbForageAction : BotAction
{
    private const int Reach = 2;
    private const int LookRange = 18;

    // Сколько раз сменить поляну, прежде чем признать, что здесь не растёт.
    private const int MaxMoves = 3;

    // Сколько тиков подряд на одном кусте, прежде чем бросить его (неудачи не губят куст,
    // но и сидеть у него вечно незачем).
    private const int MaxTriesPerHerb = 4;

    private readonly int _quota;
    private int _done;
    private int _moves;
    private int _tries;
    private int _idle;
    private MahaonWildHerb _herb;
    private GoToAction _walk;

    public HerbForageAction(int quota) => _quota = quota;

    public override void Start(BotBrain brain)
    {
        var bot = brain.Bot;
        MahaonHerbSpawner.AddForager(bot);

        // Игрок, пришедший на место, видит траву, выросшую за время его пути. Бот приходит
        // туда же — досаживаем округу сразу, а не ждём несколько тиков таймера.
        if (bot.Map != null)
        {
            for (var i = 0; i < 4; i++)
            {
                MahaonHerbSpawner.FillAround(bot, bot.Map);
            }
        }
    }

    public override void Stop(BotBrain brain)
    {
        _walk?.Stop(brain);
        MahaonHerbSpawner.RemoveForager(brain.Bot);
    }

    public override string Describe(BotBrain brain) => $"Собирает травы: {_done}/{_quota}";

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_walk != null)
        {
            var step = _walk.Tick(brain);
            if (step.Status == BotActionStatus.Running)
            {
                return step;
            }

            _walk.Stop(brain);
            _walk = null;
        }

        if (_done >= _quota)
        {
            return BotActionResult.Done();
        }

        if (_herb is { Deleted: false } && bot.InRange(_herb.GetWorldLocation(), Reach))
        {
            bot.Direction = bot.GetDirectionTo(_herb);
            var before = _herb;
            before.OnDoubleClick(bot);

            if (before.Deleted)
            {
                _done++;
                _herb = null;
                _tries = 0;
            }
            else if (++_tries >= MaxTriesPerHerb)
            {
                _herb = null;
                _tries = 0;
            }

            return BotActionResult.Running(2200); // сбор — раз в пару секунд, как у игрока
        }

        _herb = Nearest(bot);

        if (_herb == null)
        {
            // Пусто: подождать роста (таймер досаживает), а потом сменить поляну.
            if (++_idle < 4)
            {
                return BotActionResult.Running(15000);
            }

            _idle = 0;

            if (++_moves > MaxMoves || bot.Map == null ||
                !TryPickWild(bot.Map, bot.Location, 25, 50, out var next))
            {
                return _done > 0 ? BotActionResult.Done() : BotActionResult.Failed();
            }

            _walk = new GoToAction(bot.Map, next, 2, "на другую поляну");
            _walk.Start(brain);
            return BotActionResult.Running(300);
        }

        _idle = 0;

        if (!bot.InRange(_herb.GetWorldLocation(), Reach))
        {
            _walk = new GoToAction(_herb.Map, _herb.Location, 1, "к растению");
            _walk.Start(brain);
        }

        return BotActionResult.Running(300);
    }

    private static MahaonWildHerb Nearest(Mobile bot)
    {
        MahaonWildHerb best = null;
        var bestDist = double.MaxValue;

        foreach (var herb in bot.Map.GetItemsInRange<MahaonWildHerb>(bot.Location, LookRange))
        {
            // Только то, что по силам: иначе бот стоял бы у мандрагоры, которая ему не дастся.
            if (herb.Deleted || !MahaonHerbs.CanGather(bot, herb.Info))
            {
                continue;
            }

            var dist = bot.GetDistanceToSqrt(herb);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = herb;
            }
        }

        return best;
    }

    /// <summary>
    /// Случайная проходимая точка с травяной местностью на расстоянии min..max от центра —
    /// не в городе и не в доме. Пробует несколько раз и сдаётся, если вокруг одни скалы и вода.
    /// </summary>
    public static bool TryPickWild(Map map, Point3D center, int min, int max, out Point3D spot)
    {
        for (var i = 0; i < 30; i++)
        {
            var angle = Utility.RandomDouble() * System.Math.PI * 2;
            var dist = min + Utility.RandomDouble() * (max - min);
            var x = center.X + (int)System.Math.Round(System.Math.Cos(angle) * dist);
            var y = center.Y + (int)System.Math.Round(System.Math.Sin(angle) * dist);

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height ||
                MahaonHerbs.HabitatAt(map, x, y) == MahaonHabitat.None)
            {
                continue;
            }

            var z = map.GetAverageZ(x, y);
            var p = new Point3D(x, y, z);

            if (!map.CanSpawnMobile(x, y, z) || Region.Find(p, map).IsPartOf<Regions.TownRegion>() ||
                Multis.BaseHouse.FindHouseAt(p, map, 16) != null)
            {
                continue;
            }

            spot = p;
            return true;
        }

        spot = Point3D.Zero;
        return false;
    }
}
