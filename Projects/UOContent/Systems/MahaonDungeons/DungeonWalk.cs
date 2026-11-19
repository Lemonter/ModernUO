using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ModernUO.Serialization;
using Server.Commands;
using Server.Engines.Pathing.Nav;
using Server.Mobiles;

namespace Server.Systems.MahaonDungeons;

/// <summary>
/// Ходок: безликий человек для проверки подземелий. Неуязвим (монстры его не трогают), проходит
/// сквозь любых мобилей (проверяется земля, а не толкотня у входа) и считается игроком — иначе
/// телепорты его не переносят. В сохранение не попадает: после загрузки сразу удаляется.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DungeonWalker : Mobile, IPathDoorOpener
{
    // Двери открывает, как бот: без этого местный поиск считал закрытые створки стенами.
    public bool OpensDoors => true;

    public DungeonWalker()
    {
        Body = 0x190;
        Hue = 0x83EA;
        Name = "ходок";
        Player = true;
        Blessed = true;
        Str = Dex = Int = 100;
        Hits = Stam = Mana = 100;
    }

    public override bool CheckShove(Mobile shoved) => true;

    [AfterDeserialization]
    private void AfterDeserialization() => Timer.DelayCall(Delete);
}

/// <summary>
/// Пачки ходоков по подземельям: каждая начинает у наземного входа и идёт к концу — самой дальней
/// достижимой точке, плюс по ходоку к концу каждого другого уровня. Все идут маршрутами
/// навигации ботов, так что проверяется ровно то, чем ходят боты: граф, телепорты, двери, местный
/// поиск в узостях.
///
/// [DungeonCheck [all|имя|здесь] — обследование без шагов; [DungeonWalk [all|имя|здесь] [пачка] —
/// пустить пачки; [DungeonWalk status — кто где; [DungeonWalk stop — убрать ходоков.
/// Итог пишется и в Logs/dungeon-check.txt.
/// </summary>
public static class DungeonWalk
{
    private const int StepMs = 200;
    private const int MaxReplans = 8;
    private static readonly TimeSpan NoProgress = TimeSpan.FromSeconds(40);
    private static readonly TimeSpan MaxWalk = TimeSpan.FromMinutes(12);

    private enum State
    {
        Walking,
        Arrived,
        Stuck
    }

    private sealed class Walk
    {
        public string Dungeon;
        public int Number;
        public DungeonWalker Walker;
        public Map GoalMap;
        public Point3D Goal;
        public string GoalName;
        public NavFollower Follower;
        public State State;
        public int Steps, Replans;
        public DateTime Started, LastProgress;
        public string Reason;
        public Mobile Reporter;
    }

    private static readonly List<Walk> _walks = [];
    private static Timer _timer;
    private static readonly StringBuilder _log = new();

    public static void Configure()
    {
        CommandSystem.Register("DungeonCheck", AccessLevel.GameMaster, OnCheck);
        CommandSystem.Register("DungeonWalk", AccessLevel.GameMaster, OnWalk);
        CommandSystem.Register("FixTeleporters", AccessLevel.Developer, OnFixTeleporters);
    }

    private static List<DungeonSurvey.Report> Select(CommandEventArgs e, int argIndex)
    {
        var arg = e.Length > argIndex ? e.GetString(argIndex) : "здесь";
        IEnumerable<Regions.DungeonRegion> regions;

        if (arg.InsensitiveEquals("all") || arg.InsensitiveEquals("все"))
        {
            regions = DungeonSurvey.Dungeons();
        }
        else if (arg.InsensitiveEquals("здесь") || arg.InsensitiveEquals("here"))
        {
            regions = DungeonSurvey.Dungeons(e.Mobile.Map);
        }
        else
        {
            var name = string.Join(" ", e.Arguments.Skip(argIndex)).Trim();
            var map = e.Mobile.Map;
            regions = DungeonSurvey.Dungeons().Where(r => r.Name.InsensitiveEquals(name)).OrderBy(r => r.Map == map ? 0 : 1).Take(1);
        }

        return regions.Select(DungeonSurvey.Survey).ToList();
    }

    [Usage("DungeonCheck [all|здесь|имя]")]
    [Description("Обследует подземелья: вход, проходимость настоящими шагами, дороги ботов, отрезанные куски, камень путешествий.")]
    private static void OnCheck(CommandEventArgs e)
    {
        var reports = Select(e, 0);
        var log = new StringBuilder();

        foreach (var r in reports)
        {
            foreach (var line in Describe(r))
            {
                e.Mobile.SendMessage(Verdict(r) == null ? 0x3B2 : 0x22, line);
                log.AppendLine(line);
            }
        }

        var broken = reports.Where(r => Verdict(r) != null).ToList();
        var summary = $"Подземелий {reports.Count}, с бедой {broken.Count}: {string.Join(", ", broken.Select(r => $"{r.Name} ({r.Map.Name})"))}";
        e.Mobile.SendMessage(0x44, summary);
        log.AppendLine(summary);
        WriteLog("dungeon-check.txt", log);
    }

    /// <summary>Чем подземелье нехорошо, или null. Куски за особым входом (квест, ключ) поломкой не считаются.</summary>
    private static string Verdict(DungeonSurvey.Report r)
    {
        if (!r.HasGraph)
        {
            return "у фасета нет графа навигации";
        }

        if (!r.HasEntrance)
        {
            return "нет входа";
        }

        if (r.DeadTeleporters.Count > 0)
        {
            return $"мёртвых телепортов: {r.DeadTeleporters.Count}";
        }

        if (r.RealCells < 30 && r.GatedCells < 30)
        {
            return "от входа никуда не пройти";
        }

        var lost = r.CutOff.FirstOrDefault(p => p.Note == null && p.Cells >= 150);
        if (lost != null)
        {
            return $"отрезан кусок у {lost.Location} ({lost.Cells} клеток)";
        }

        if (r.GraphHoleCells >= 50)
        {
            return $"боты не находят дороги в {r.GraphHoleCells} клеток, куда человек дойдёт";
        }

        if (!r.StoneReached)
        {
            return r.StoneOnGround ? "камень ведёт в отрезанную часть" : "камень ведёт в пустоту";
        }

        return null;
    }

    private static IEnumerable<string> Describe(DungeonSurvey.Report r)
    {
        var verdict = Verdict(r);
        yield return $"== {r.Name} ({r.Map.Name}){(verdict == null ? "" : $" — {verdict}")}";

        if (r.Entrances.Count > 0)
        {
            var e = r.Entrances[0];
            yield return $"   вход: {e.Kind} {e.SourceMap?.Name} {e.Source} -> {e.Destination} (всего {r.Entrances.Count})";
        }

        foreach (var g in r.GatedEntrances.Take(3))
        {
            yield return $"   особый вход: {g.Kind} {g.SourceMap?.Name} {g.Source} -> {g.Destination}";
        }

        yield return $"   шагами от входа: {r.RealCells} клеток (+{r.GatedCells} за особыми телепортами), телепортов внутри {r.TeleportersInside}; " +
                     $"боты не знают дороги в {r.GraphHoleCells}";
        yield return $"   уровней {r.LevelEnds.Count}, конец {r.Far?.ToString() ?? "-"} (путь {r.FarCost / 10})";

        foreach (var b in r.Breaks.Take(2))
        {
            yield return "   разрыв графа: " + b;
        }

        foreach (var dead in r.DeadTeleporters.Take(4))
        {
            yield return "   мёртвый телепорт (высота не совпадает с полом): " + dead;
        }

        var cut = r.CutOff.Take(4).ToList();
        if (cut.Count > 0)
        {
            yield return "   отрезано: " + string.Join("; ", cut.Select(p => $"{p.Location} {p.Cells} кл.{(p.Note == null ? "" : " " + p.Note)}"));
        }

        yield return $"   камень: {r.Stone} — {(r.StoneReached ? "на пути" : r.StoneOnGround ? "в отрезанной части" : "не на земле")}";
    }

    [Usage("FixTeleporters")]
    [Description("Ставит телепорты, лежащие не на высоте пола, на ближайшую поверхность: шаг на них снова переносит.")]
    private static void OnFixTeleporters(CommandEventArgs e)
    {
        var list = DungeonSurvey.RepairTeleporters();
        foreach (var line in list.Take(30))
        {
            e.Mobile.SendMessage(line);
        }

        e.Mobile.SendMessage(0x44, $"Телепортов поправлено: {list.Count}.");
    }

    [Usage("DungeonWalk [all|здесь|имя] [пачка] | status | stop")]
    [Description("Пускает пачки ходоков от входа подземелья к его концу и к концам уровней.")]
    private static void OnWalk(CommandEventArgs e)
    {
        var first = e.Length > 0 ? e.GetString(0) : "здесь";

        if (first.InsensitiveEquals("status"))
        {
            Status(e.Mobile);
            return;
        }

        if (first.InsensitiveEquals("stop"))
        {
            StopAll();
            e.Mobile.SendMessage("Ходоки убраны.");
            return;
        }

        var pack = 2;
        var args = e.Arguments.ToList();
        if (args.Count > 1 && int.TryParse(args[^1], out var n))
        {
            pack = Math.Clamp(n, 1, 8);
            args.RemoveAt(args.Count - 1);
        }

        var reports = Select(new CommandEventArgs(e.Mobile, e.Command, string.Join(" ", args), args.ToArray()), 0);

        // «all» для ходоков — подземелья камня путешествий: открытые зоны и служебные куски
        // регионов пачками гонять незачем.
        if (args.Count > 0 && (args[0].InsensitiveEquals("all") || args[0].InsensitiveEquals("все")))
        {
            var stone = new HashSet<(string, Map)>(
                Gumps.DungeonTravelTargets.Felucca.Concat(Gumps.DungeonTravelTargets.Trammel).Concat(Gumps.DungeonTravelTargets.Ilshenar)
                    .Concat(Gumps.DungeonTravelTargets.Malas).Concat(Gumps.DungeonTravelTargets.TerMur).Select(t => (t.name, t.map))
            );
            reports = reports.Where(r => stone.Contains((r.Name, r.Map))).ToList();
        }
        if (_walks.Count == 0)
        {
            _log.Clear();
        }

        var started = 0;

        foreach (var r in reports)
        {
            if (r.Start is not { } start || r.Far is not { } far)
            {
                e.Mobile.SendMessage(0x22, $"{r.Name} ({r.Map.Name}): идти неоткуда или некуда — {Verdict(r) ?? "нет конца"}");
                _log.AppendLine($"{r.Name} ({r.Map.Name}): не пущены — {Verdict(r) ?? "нет конца"}");
                continue;
            }

            var number = 0;
            for (var i = 0; i < pack; i++)
            {
                started += Launch(e.Mobile, r, start, far, "конец", ++number) ? 1 : 0;
            }

            foreach (var (loc, _) in r.LevelEnds.Skip(1).Take(2))
            {
                started += Launch(e.Mobile, r, start, loc, "конец уровня", ++number) ? 1 : 0;
            }
        }

        e.Mobile.SendMessage(0x44, $"Пущено ходоков: {started} в {reports.Count} подземелий.");

        if (started > 0 && _timer == null)
        {
            _timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMilliseconds(StepMs), Tick);
        }
    }

    private static bool Launch(Mobile reporter, DungeonSurvey.Report r, Point3D start, Point3D goal, string goalName, int number)
    {
        var walker = new DungeonWalker { Name = $"ходок {r.Name} {number}" };
        walker.MoveToWorld(start, r.StartMap);

        var walk = new Walk
        {
            Dungeon = $"{r.Name} ({r.Map.Name})",
            Number = number,
            Walker = walker,
            GoalMap = r.Map,
            Goal = goal,
            GoalName = goalName,
            Started = Core.Now,
            LastProgress = Core.Now,
            Reporter = reporter
        };

        if (!Replan(walk))
        {
            Finish(walk, State.Stuck, "маршрута нет с самого начала");
            return false;
        }

        _walks.Add(walk);
        return true;
    }

    private static bool Replan(Walk w)
    {
        var route = NavPathfinder.Find(w.Walker.Map, w.Walker.Location, w.GoalMap, w.Goal);
        if (route == null)
        {
            return false;
        }

        w.Follower = new NavFollower(w.Walker, route, 2);
        return true;
    }

    private static void Tick()
    {
        var now = Core.Now;

        for (var i = _walks.Count - 1; i >= 0; i--)
        {
            var w = _walks[i];
            if (w.State != State.Walking)
            {
                continue;
            }

            if (w.Walker.Deleted)
            {
                Finish(w, State.Stuck, "ходок удалён");
                continue;
            }

            if (now - w.Started > MaxWalk)
            {
                Finish(w, State.Stuck, "не дошёл за 12 минут");
                continue;
            }

            if (now - w.LastProgress > NoProgress)
            {
                Finish(w, State.Stuck, "40 секунд ни шага");
                continue;
            }

            switch (w.Follower.Step(true))
            {
                case NavStepResult.Moved:
                    {
                        w.Steps++;
                        w.LastProgress = now;
                        break;
                    }
                case NavStepResult.Arrived:
                    {
                        Finish(w, State.Arrived, null);
                        break;
                    }
                case NavStepResult.Failed:
                    {
                        if (++w.Replans > MaxReplans)
                        {
                            Finish(w, State.Stuck, $"маршрут рвётся ({w.Replans} перестроек)");
                        }
                        else if (!Replan(w))
                        {
                            Finish(w, State.Stuck, "отсюда маршрута нет");
                        }

                        break;
                    }
            }
        }

        if (_walks.All(w => w.State != State.Walking))
        {
            _timer?.Stop();
            _timer = null;
            Summary(null);
        }
    }

    private static void Finish(Walk w, State state, string reason)
    {
        w.State = state;
        w.Reason = reason;

        var at = w.Walker.Deleted ? "?" : $"{w.Walker.Location} {w.Walker.Map?.Name} [{w.Walker.Region?.Name}]";
        if (state == State.Stuck && !w.Walker.Deleted && w.Walker.Map != null)
        {
            var free = 0;
            for (var d = 0; d < 8; d++)
            {
                free += Movement.Movement.CheckMovement(w.Walker, (Direction)d, out _) ? 1 : 0;
            }

            var next = w.Follower?.Route.Waypoints.Skip(w.Follower.Index).Select(p => (NavWaypoint?)p).FirstOrDefault();
            at += $" (ходов отсюда {free}/8, средняя высота земли {w.Walker.Map.GetAverageZ(w.Walker.X, w.Walker.Y)}" +
                  $"{(next is not { } n ? "" : $", следующая точка {n.Location} {n.Kind}")})";
        }
        var secs = (Core.Now - w.Started).TotalSeconds;
        var line = state == State.Arrived
            ? $"[ходок] {w.Dungeon} №{w.Number}: дошёл ({w.GoalName}) {w.Goal} — {w.Steps} шагов, {secs:F0} с, перестроек {w.Replans}"
            : $"[ходок] {w.Dungeon} №{w.Number}: ЗАСТРЯЛ ({w.GoalName} {w.Goal}) у {at} — {reason}; {w.Steps} шагов, {secs:F0} с";

        _log.AppendLine(line);
        if (w.Reporter is { Deleted: false, NetState: not null })
        {
            w.Reporter.SendMessage(state == State.Arrived ? 0x44 : 0x22, line);
        }

        if (!w.Walker.Deleted)
        {
            w.Walker.Delete();
        }
    }

    private static void Status(Mobile to)
    {
        var walking = _walks.Where(w => w.State == State.Walking).ToList();
        to.SendMessage($"Идут: {walking.Count}, дошли: {_walks.Count(w => w.State == State.Arrived)}, застряли: {_walks.Count(w => w.State == State.Stuck)}");
        foreach (var w in walking.Take(12))
        {
            to.SendMessage($"  {w.Dungeon} №{w.Number}: {w.Walker.Location} [{w.Walker.Region?.Name}] шагов {w.Steps}");
        }
    }

    private static void Summary(Mobile to)
    {
        var byDungeon = _walks.GroupBy(w => w.Dungeon).OrderBy(g => g.Key);
        var bad = new List<string>();
        foreach (var g in byDungeon)
        {
            var stuck = g.Count(w => w.State != State.Arrived);
            if (stuck > 0)
            {
                bad.Add($"{g.Key} ({stuck}/{g.Count()})");
            }
        }

        var line = $"[ходоки] итог: подземелий {byDungeon.Count()}, с застрявшими {bad.Count}: {string.Join(", ", bad)}";
        _log.AppendLine(line);

        var reporter = _walks.Select(w => w.Reporter).FirstOrDefault(m => m is { Deleted: false, NetState: not null });
        (to ?? reporter)?.SendMessage(0x44, line);
        WriteLog("dungeon-walk.txt", _log);
        _walks.Clear();
    }

    private static void StopAll()
    {
        foreach (var w in _walks.Where(w => w.State == State.Walking))
        {
            Finish(w, State.Stuck, "остановлен");
        }

        _walks.Clear();
        _timer?.Stop();
        _timer = null;
    }

    private static void WriteLog(string file, StringBuilder log)
    {
        try
        {
            var dir = Path.Combine(Core.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, file), log.ToString());
        }
        catch (IOException)
        {
            // Отчёт в файле — удобство; в игре он уже показан.
        }
    }
}
