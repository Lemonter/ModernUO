using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Движение ботов: один слой, через который ходят все занятия.
///
///     ---- Почему боты вставали через несколько тайлов ----
///
///     Движок предоставляет ровно один поисковик пути — BitmapAStarAlgorithm, и у него
///     два свойства, о которые разбивалась прежняя реализация.
///
///     Первое известно: AreaSize = 38, и CheckCondition честно отказывается искать, если
///     старт и цель дальше 38 тайлов друг от друга.
///
///     Второе куда важнее и в комментариях нигде не было записано: окно поиска — это
///     квадрат 38×38, ЦЕНТРИРОВАННЫЙ МЕЖДУ стартом и целью (см. _xOffset/_yOffset в
///     BitmapAStarAlgorithm.Find). При отрезке длиной 30 тайлов такое окно оставляет по
///     четыре тайла в стороны — и обход, которому нужно уйти вбок дальше, просто не
///     существует внутри окна. A* возвращает неудачу.
///
///     А дальше срабатывает третье, и это главная ловушка: PathFollower.Follow при
///     неуспешном пути НЕ останавливается. Он делает шаг GetDirectionTo(goal) — то есть
///     идёт на цель по прямой, без всякого обхода. Вот это и выглядело как «прошёл
///     несколько тайлов и упёрся»: маршрут не нашёлся, и движок молча повёл бота в стену.
///
///     Прежний код усугублял всё тем, что промежуточную точку выбирал по CanSpawnMobile —
///     «сюда можно встать». Стоять там можно, а вот дойти — вопрос, который никто не
///     задавал. И если веер не нашёл ни одной подходящей точки, отдавалась точка на прямой
///     линии вообще без проверок.
///
///     ---- Что делается вместо этого ----
///
///     1. Маршрут режется на отрезки не длиннее MaxLegLength (20). Окно A* при этом
///        оставляет по девять тайлов в стороны — достаточно, чтобы обойти здание.
///     2. Промежуточная точка принимается ТОЛЬКО если MovementPath до неё вернул Success.
///        То есть в PathFollower никогда не попадает цель, к которой нет маршрута, и его
///        слепой шаг по прямой не запускается в принципе.
///     3. Если ни один кандидат не прошёл проверку несколько раз подряд — цель признаётся
///        недостижимой, а не «пойдём напролом и посмотрим».
///     4. Застревание определяется по неподвижности, а не по расстоянию до цели: идя в
///        обход вдоль стены, бот законно не приближается к цели по прямой.
///
///     Свой A* при этом не пишется: движковый работает, ему просто нужно давать задачи,
///     которые он способен решить.
/// </summary>
public partial class BotController
{
    /// <summary>
    ///     Длина одного отрезка маршрута. Меньше 38 намеренно и с запасом: окно поиска —
    ///     квадрат 38×38 посередине между концами отрезка, и разница между 38 и этим
    ///     числом это и есть место для обхода вбок.
    /// </summary>
    private const int MaxLegLength = 20;

    /// <summary>Сколько тиков подряд бот может простоять на месте, прежде чем маршрут
    /// признаётся негодным. Тик движения — четверть секунды.</summary>
    private const int MotionlessRepathTicks = 3;

    /// <summary>Сколько раз подряд поиск промежуточной точки может ничего не найти,
    /// прежде чем цель объявляется недостижимой.</summary>
    private const int WaypointFailuresBeforeGivingUp = 3;

    /// <summary>
    ///     Сколько кандидатов проверять настоящим поиском пути за один пересчёт. Каждая
    ///     проверка — полноценный A*, поэтому перебирать весь веер накладно.
    ///
    ///     Двенадцать, а не семь: теперь на каждое направление приходится по три длины, и
    ///     семи хватало ровно на два с небольшим направления. Расход при этом упал, а не
    ///     вырос — короткий прямой отрезок находится обычно с первой-третьей попытки, и
    ///     бюджет тратится целиком только в по-настоящему трудных местах.
    /// </summary>
    private const int MaxWaypointProbes = 12;

    /// <summary>Забвение неудачной точки: препятствие могло исчезнуть.</summary>
    private static readonly TimeSpan FailedWaypointLifetime = TimeSpan.FromMinutes(3);

    /// <summary>Сколько неудачных точек помнить.</summary>
    private const int MaxRememberedFailures = 12;

    /// <summary>
    ///     Сколько отрезков подряд можно пройти, ни разу не подобравшись к цели ближе, чем
    ///     уже подбирались.
    ///
    ///     Застревание ловилось только по неподвижности, а бот, ходящий кругами, двигается
    ///     непрерывно и бодро. Формально он не застрял ни на секунду — и молотил так до
    ///     истечения общего срока на дорогу, то есть четыре минуты на цель, до которой
    ///     дороги нет. Теперь смотрим ещё и на то, приближается ли он вообще.
    /// </summary>
    private const int LegsWithoutProgressBeforeGivingUp = 6;

    /// <summary>Насколько должно сократиться расстояние, чтобы счесть это продвижением.</summary>
    private const double ProgressEpsilon = 1.0;

    /// <summary>Общий срок на дорогу. Страховка от бота, который бодро ходит кругами и
    /// формально не застревает ни на секунду.</summary>
    private static readonly TimeSpan TripDeadline = TimeSpan.FromMinutes(4);

    /// <summary>Отклонения от направления на цель, в радианах: прямо, чуть вправо, чуть
    /// влево, сильнее, ещё сильнее, и наконец вбок почти под прямым углом.</summary>
    private static readonly double[] _legBearings =
    {
        0, 0.4, -0.4, 0.85, -0.85, 1.3, -1.3, 1.9, -1.9
    };

    /// <summary>Длины пробных отрезков — если полная не проходит, годится и короткая.</summary>
    private static readonly int[] _legLengths = { MaxLegLength, MaxLegLength * 2 / 3, MaxLegLength / 3 };

    /// <summary>Диагностика в консоль. Включается настройкой bots.pathDebug.</summary>
    public static bool PathDebug { get; set; }

    /// <summary>
    ///     Шаг к цели. Возвращает true, когда бот действительно ДОШЁЛ до неё — не до
    ///     промежуточной точки.
    ///
    ///     Единая точка входа для всех занятий: добыча, охота, торговля, банк, переезды,
    ///     погоня. Ничего из этого переписывать не потребовалось — подпись не изменилась.
    /// </summary>
    private static bool StepTowardPath(PlayerMobile bot, BotProfile profile, Point3D destination)
    {
        if (bot.Spell?.IsCasting == true && !bot.Mounted)
        {
            return false;
        }

        if (bot.Map == null || bot.Map == Map.Internal)
        {
            return false;
        }

        // Смена цели обнуляет всё: старые отрезки, счётчики и список тупиков к новой цели
        // отношения не имеют.
        if (profile.PathDestination != destination)
        {
            profile.ResetPathing();
            profile.PathDestination = destination;
            profile.PathDeadline = Core.Now + TripDeadline;
            profile.LastMoveCheckPosition = bot.Location;
            profile.TripStarted = Core.Now;
            profile.TripOrigin = bot.Location;

            Debug(
                bot,
                $"новая цель {destination}, отсюда {bot.Location}, " +
                $"по прямой {bot.GetDistanceToSqrt(destination):F0} тайлов"
            );
        }

        if (Arrived(bot, destination))
        {
            TryTriggerTeleporterAt(bot, bot.Location, bot.Map);
            DebugTripEnd(bot, profile, destination, "дошёл");
            profile.ResetPathing();

            return true;
        }

        if (profile.PathDeadline != DateTime.MinValue && Core.Now > profile.PathDeadline)
        {
            return GiveUp(bot, profile, destination, "вышло время на дорогу");
        }

        // ---- Застревание: смотрим, сдвинулся ли бот на самом деле -----------------------
        if (bot.Location == profile.LastMoveCheckPosition)
        {
            profile.MotionlessTicks++;
        }
        else
        {
            profile.MotionlessTicks = 0;
            profile.LastMoveCheckPosition = bot.Location;

            var distanceNow = bot.GetDistanceToSqrt(destination);

            if (distanceNow < profile.BestGoalDistance)
            {
                profile.BestGoalDistance = distanceNow;
            }
        }

        var stuck = profile.MotionlessTicks >= MotionlessRepathTicks;

        // ---- Нужен ли новый отрезок ------------------------------------------------------
        var needLeg = profile.ActivePath == null ||
                      stuck ||
                      Arrived(bot, profile.ActivePathGoal);

        if (needLeg)
        {
            if (stuck && profile.ActivePathGoal != default)
            {
                // Этот отрезок довести не вышло — больше его не предлагать какое-то время.
                RememberFailedWaypoint(profile, profile.ActivePathGoal);
                Debug(
                    bot,
                    $"стою {profile.MotionlessTicks} тиков на {bot.Location} — " +
                    $"бракую отрезок до {profile.ActivePathGoal} (в чёрном списке {profile.FailedWaypoints.Count})"
                );
            }

            // Продвинулись ли мы к цели с прошлого отрезка. Считаем именно на границе
            // отрезков, а не каждый тик: идя в обход вдоль стены, бот законно не
            // приближается по прямой, и потиковая проверка сочла бы это кружением.
            var distanceNow = bot.GetDistanceToSqrt(destination);

            if (distanceNow < profile.BestGoalDistance - ProgressEpsilon)
            {
                profile.BestGoalDistance = distanceNow;
                profile.LegsWithoutProgress = 0;
            }
            else if (profile.ActivePath != null)
            {
                profile.LegsWithoutProgress++;

                if (profile.LegsWithoutProgress >= LegsWithoutProgressBeforeGivingUp)
                {
                    return GiveUp(
                        bot, profile, destination,
                        $"хожу кругами: {profile.LegsWithoutProgress} отрезков без приближения " +
                        $"(лучшее расстояние {profile.BestGoalDistance:F0}, сейчас {distanceNow:F0})"
                    );
                }
            }

            if (!TryStartLeg(bot, profile, destination))
            {
                profile.WaypointSearchFailures++;

                if (profile.WaypointSearchFailures >= WaypointFailuresBeforeGivingUp)
                {
                    return GiveUp(bot, profile, destination, "ни одного отрезка с настоящим маршрутом");
                }

                // Ещё не сдаёмся, но и напролом не идём: постоим тик, попробуем снова —
                // мешать может чужой мобиль, который сейчас отойдёт.
                profile.MotionlessTicks = 0;

                return false;
            }

            profile.WaypointSearchFailures = 0;
            profile.MotionlessTicks = 0;
        }

        // ---- Шаг ------------------------------------------------------------------------
        profile.ActivePath.Follow(0);

        if (!Arrived(bot, destination))
        {
            return false;
        }

        TryTriggerTeleporterAt(bot, bot.Location, bot.Map);
        profile.ResetPathing();

        return true;
    }

    /// <summary>Дошёл ли бот до точки. Двух тайлов достаточно: тайл в тайл движок не
    /// всегда встаёт из-за перепадов высоты.</summary>
    private static bool Arrived(Mobile bot, Point3D point) =>
        point != default && bot.GetDistanceToSqrt(point) < 2;

    /// <summary>
    ///     Начинает новый отрезок: подбирает точку, до которой ЕСТЬ маршрут, и заводит на
    ///     неё PathFollower.
    /// </summary>
    private static bool TryStartLeg(PlayerMobile bot, BotProfile profile, Point3D destination)
    {
        if (!TryFindReachableWaypoint(bot, profile, destination, out var waypoint))
        {
            return false;
        }

        profile.ActivePath = new PathFollower(bot, waypoint);
        profile.ActivePathGoal = waypoint;
        profile.TripLegs++;

        Debug(
            bot,
            $"отрезок #{profile.TripLegs}: {bot.Location} → {waypoint} " +
            $"(длина {profile.LastLegLength}, отклонение {Degrees(profile.LastLegTurn)}°, " +
            $"A*={profile.LastLegProbes}; до цели {bot.GetDistanceToSqrt(destination):F0})"
        );

        return true;
    }

    /// <summary>
    ///     Точка, до которой у бота действительно есть дорога.
    ///
    ///     Проверка — не CanSpawnMobile («сюда можно встать»), а построение настоящего
    ///     маршрута тем же алгоритмом, которым потом пойдёт PathFollower. Только так
    ///     гарантируется, что движок не сорвётся на слепой шаг по прямой.
    /// </summary>
    private static bool TryFindReachableWaypoint(
        Mobile bot, BotProfile profile, Point3D destination, out Point3D waypoint
    )
    {
        waypoint = default;

        var map = bot.Map;

        if (map == null)
        {
            return false;
        }

        PruneFailedWaypoints(profile);

        // Цель близко — идём прямо к ней, отрезок не нужен.
        if (bot.GetDistanceToSqrt(destination) <= MaxLegLength && IsRoutable(bot, destination))
        {
            waypoint = destination;
            return true;
        }

        var dx = destination.X - bot.X;
        var dy = destination.Y - bot.Y;
        var bearing = Math.Atan2(dy, dx);
        var probes = 0;

        // Счётчики отказов — только ради журнала: по ним сразу видно, во что упёрлись.
        int offMap = 0, blacklisted = 0, occupied = 0, unroutable = 0;

        // ВНИМАНИЕ на порядок вложенности: сначала направление, потом длина.
        //
        // Раньше было наоборот — все девять направлений на полной длине, потом на двух
        // третях, потом на трети. И это ломало всё, что дальше MaxLegLength: бюджет в
        // MaxWaypointProbes полноценных A* целиком уходил на длинные отрезки, а до коротких
        // перебор не доживал никогда. В городе длинный отрезок почти всегда упирается в
        // дом, так что бот объявлял цель недостижимой, ни разу не попробовав шагнуть на
        // шесть тайлов вперёд. До двадцати тайлов работало только потому, что туда ведёт
        // короткое замыкание выше (идём прямо к цели), а дальше начинался этот перебор.
        //
        // Теперь прямое направление получает все три длины подряд, и лишь когда прямо не
        // выходит вовсе — пробуем отклоняться. Отсюда же и «криво»: отклонение в 23° на
        // двадцати тайлах видно глазом, а короткий прямой шаг — нет.
        // Дальше цели не шагаем ни при каких обстоятельствах.
        //
        // Из-за этого боты и ходили кругами. Отрезок строился фиксированной длины, и если
        // до цели было двенадцать тайлов, а отрезок двадцать — бот проезжал цель насквозь.
        // С той стороны цель оказывалась позади, направление разворачивалось, и он ехал
        // обратно, снова мимо. В журнале это выглядело так: [2500] → [2520], потом
        // [2519] → [2499], и по кругу до самого истечения срока на дорогу.
        //
        // Само по себе короткое замыкание «цель близко — идём прямо к ней» выше от этого не
        // спасает: оно срабатывает только если до ЦЕЛИ есть маршрут. А когда цель стоит в
        // недоступном месте — внутри дома, на скале, — замыкание не срабатывает, и веер
        // честно отправлял бота за горизонт.
        var toGoal = Math.Max(1, (int)bot.GetDistanceToSqrt(destination));

        foreach (var turn in _legBearings)
        {
            foreach (var rawLength in _legLengths)
            {
                if (probes >= MaxWaypointProbes)
                {
                    DebugProbeFailure(bot, probes, offMap, blacklisted, occupied, unroutable, "бюджет зондов");
                    return false;
                }

                var length = Math.Min(rawLength, toGoal);
                var angle = bearing + turn;
                var wx = bot.X + (int)Math.Round(Math.Cos(angle) * length);
                var wy = bot.Y + (int)Math.Round(Math.Sin(angle) * length);

                if (wx < 0 || wy < 0 || wx >= map.Width || wy >= map.Height)
                {
                    offMap++;
                    continue;
                }

                var candidate = new Point3D(wx, wy, map.GetAverageZ(wx, wy));

                // Дешёвые проверки первыми, чтобы зря не гонять A*.
                if (IsRecentFailure(profile, candidate))
                {
                    blacklisted++;
                    continue;
                }

                if (!map.CanSpawnMobile(candidate))
                {
                    occupied++;
                    continue;
                }

                probes++;
                profile.TripProbes++;

                if (!IsRoutable(bot, candidate))
                {
                    unroutable++;
                    continue;
                }

                waypoint = candidate;
                profile.LastLegTurn = turn;
                profile.LastLegLength = length;
                profile.LastLegProbes = probes;

                return true;
            }
        }

        DebugProbeFailure(bot, probes, offMap, blacklisted, occupied, unroutable, "перебор исчерпан");

        return false;
    }

    private static void DebugProbeFailure(
        Mobile bot, int probes, int offMap, int blacklisted, int occupied, int unroutable, string why
    )
    {
        Debug(
            bot,
            $"отрезок не найден ({why}): A*={probes}, вне карты={offMap}, " +
            $"в чёрном списке={blacklisted}, негде встать={occupied}, нет маршрута={unroutable}"
        );
    }

    /// <summary>Есть ли от бота реальный маршрут до точки — тот же MovementPath, которым
    /// пользуется PathFollower, только без движения.</summary>
    private static bool IsRoutable(Mobile bot, Point3D point) =>
        bot.GetDistanceToSqrt(point) < 2 || new MovementPath(bot, point).Success;

    private static void RememberFailedWaypoint(BotProfile profile, Point3D waypoint)
    {
        for (var i = 0; i < profile.FailedWaypoints.Count; i++)
        {
            if (profile.FailedWaypoints[i].loc == waypoint)
            {
                profile.FailedWaypoints[i] = (waypoint, Core.Now);
                return;
            }
        }

        if (profile.FailedWaypoints.Count >= MaxRememberedFailures)
        {
            profile.FailedWaypoints.RemoveAt(0);
        }

        profile.FailedWaypoints.Add((waypoint, Core.Now));
    }

    private static void PruneFailedWaypoints(BotProfile profile)
    {
        for (var i = profile.FailedWaypoints.Count - 1; i >= 0; i--)
        {
            if (Core.Now - profile.FailedWaypoints[i].at >= FailedWaypointLifetime)
            {
                profile.FailedWaypoints.RemoveAt(i);
            }
        }
    }

    private static bool IsRecentFailure(BotProfile profile, Point3D candidate)
    {
        foreach (var (loc, _) in profile.FailedWaypoints)
        {
            if (Math.Abs(loc.X - candidate.X) <= 2 && Math.Abs(loc.Y - candidate.Y) <= 2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Дороги нет. Домой не телепортируем — просто бросаем цель, запоминаем место и
    ///     отдаём решение занятию: пусть выберет другую.
    /// </summary>
    private static bool GiveUp(PlayerMobile bot, BotProfile profile, Point3D destination, string reason)
    {
        DebugTripEnd(bot, profile, destination, $"СДАЮСЬ — {reason}");

        BotWorldKnowledge.MarkUnreachable(bot.Map, destination);
        (bot as BotMobile)?.ForgetSpotNear(destination);

        profile.ResetPathing();
        profile.GoalUnreachable = true;
        profile.Activity = BotActivity.Idle;

        return false;
    }

    /// <summary>
    ///     Диагностика маршрута.
    ///
    ///     Идёт в два места сразу. В консоль — когда включён PathDebug ([BotPathDebug]),
    ///     это для всех ботов разом. И в журнал самому боту, если у него есть подключение —
    ///     а оно есть ровно в одном случае: когда человек сидит в собственном персонаже
    ///     через [BecomeBot. Тогда отладку видно изнутри и без разбора консольной простыни,
    ///     и она сама собой ограничена тем ботом, за которым смотрят.
    ///
    ///     Второй канал намеренно не гейтится общим флагом: [BecomeBot и включают затем,
    ///     чтобы посмотреть, что происходит.
    /// </summary>
    private static void Debug(Mobile bot, string message)
    {
        if (PathDebug)
        {
            Console.WriteLine($"[BotPath] {bot.Name} ({bot.Serial}): {message}");
        }

        if (bot.NetState != null)
        {
            bot.SendMessage(0x3B2, $"[путь] {message}");
        }
    }

    /// <summary>Угол в градусах — в журнале читается куда лучше радиан.</summary>
    private static int Degrees(double radians) => (int)Math.Round(radians * 180.0 / Math.PI);

    /// <summary>
    ///     Итог поездки одной строкой. Это то, ради чего заведены счётчики: «не дошёл»
    ///     и «дошёл, но построил тридцать отрезков вместо трёх» — совершенно разные болезни,
    ///     а по отдельным шагам они неразличимы.
    /// </summary>
    private static void DebugTripEnd(Mobile bot, BotProfile profile, Point3D destination, string outcome)
    {
        if (profile.TripStarted == DateTime.MinValue)
        {
            return; // цель была достигнута сразу, поездки как таковой не было
        }

        var elapsed = Core.Now - profile.TripStarted;
        var straight = profile.TripOrigin == default
            ? 0
            : Math.Sqrt(
                Math.Pow(destination.X - profile.TripOrigin.X, 2) +
                Math.Pow(destination.Y - profile.TripOrigin.Y, 2)
            );

        Debug(
            bot,
            $"{outcome}. Цель {destination}, по прямой было {straight:F0} тайлов; " +
            $"отрезков {profile.TripLegs}, A* {profile.TripProbes}, время {elapsed.TotalSeconds:F0} с"
        );
    }
}
