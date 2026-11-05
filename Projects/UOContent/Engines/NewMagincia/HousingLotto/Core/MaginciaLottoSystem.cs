/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Core/MaginciaLottoSystem.cs).
///
///     Лотерея на участки под дома на Новой Маджинции: список участков, свободные зоны застройки, очередь
///     сообщений игрокам и минутный тик, который разыгрывает участки и истекает жетоны.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             В ServUO это был предмет-контроллер, сериализовавший участки внутри себя. Здесь состояние
///             (участки, свободные зоны, очередь сообщений, золотой сток) пишет
///             <see cref="MaginciaLottoPersistence" /> — это же и есть «plot/writ state in GenericPersistence».
///             Мир при этом наполняется идемпотентно после <c>ServerStarted</c>: если участков нет, они
///             создаются вместе с камнями.
///         </item>
///         <item>
///             <c>Timer.DelayCall(TimeSpan, TimeSpan, TimerCallback)</c> → <c>Timer.StartTimer</c> с токеном,
///             который отменяется вместе с системой.
///         </item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />; <c>ColUtility.Free</c> в этом репозитории нет.</item>
///         <item>
///             <c>[CommandProperty]</c> на предмете-контроллере больше нет: <see cref="Enabled" /> и
///             <see cref="Instance" /> доступны статически, а состояние участков видно в <c>LottoTrackingGump</c>.
///         </item>
///         <item>
///             <c>FormatSextant</c> не портирован: портированные окна показывают координаты напрямую, а
///             <c>Sextant.Format</c> в этом репозитории имеет другую сигнатуру.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Accounting;
using Server.Gumps;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

public class MaginciaLottoSystem
{
    public static readonly TimeSpan DefaultLottoDuration = TimeSpan.FromDays(30);
    public static readonly int WritExpirePeriod = 30;
    public static readonly bool AutoResetLotto = false;

    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(MaginciaLottoSystem));

    private static readonly List<MaginciaHousingPlot> _plots = new();

    private static readonly Dictionary<Map, List<Rectangle2D>> _freeHousingZones = new()
    {
        [Map.Trammel] = new List<Rectangle2D>(),
        [Map.Felucca] = new List<Rectangle2D>()
    };

    private static readonly Dictionary<Mobile, List<NewMaginciaMessage>> _messageQueue = new();

    private static MaginciaLottoPersistence _persistence;
    private static TimerExecutionToken _tickToken;

    public static bool Enabled { get; set; } = true;

    public static TimeSpan LottoDuration { get; set; } = DefaultLottoDuration;

    public static int GoldSink { get; set; }

    public static List<MaginciaHousingPlot> Plots => _plots;

    public static Dictionary<Map, List<Rectangle2D>> FreeHousingZones => _freeHousingZones;

    public static Dictionary<Mobile, List<NewMaginciaMessage>> MessageQueue => _messageQueue;

    public static void Configure() => _persistence = new MaginciaLottoPersistence();

    public static void Initialize() => EventSink.ServerStarted += OnServerStarted;

    private static void OnServerStarted() => Timer.StartTimer(TimeSpan.FromSeconds(25), EnsureSetup);

    /// <summary>
    ///     Идемпотентное наполнение мира: если участков нет (первый запуск или потерянный сейв), они
    ///     создаются заново вместе с камнями. Загруженное состояние уже содержит участки и камни, поэтому
    ///     повторно ничего не ставится.
    /// </summary>
    private static void EnsureSetup()
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            var created = LoadMissingPlots();

            if (created > 0)
            {
                StartTimer();
                _logger.Information("New Magincia housing lottery is set up ({Count} plots placed)", created);
            }
            else if (_plots.Count > 0)
            {
                StartTimer();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "New Magincia housing lottery setup failed");
        }
    }

    private static int LoadMissingPlots()
    {
        var created = 0;

        for (var i = 0; i < MaginciaHousingZones.Zones.Length; i++)
        {
            var identifier = MaginciaHousingZones.Identifiers[i];
            var zone = MaginciaHousingZones.Zones[i];
            var prime = MaginciaHousingZones.IsPrime(i);

            foreach (var map in new[] { Map.Trammel, Map.Felucca })
            {
                // Участок уже разыгран либо освобождён под обычную застройку — второй раз не создаём.
                if (HasPlot(identifier, map) || _freeHousingZones[map].Contains(zone))
                {
                    continue;
                }

                var plot = new MaginciaHousingPlot(identifier, zone, prime, map)
                {
                    LottoEnds = Core.Now + LottoDuration
                };

                RegisterPlot(plot);
                plot.AddPlotStone(MaginciaHousingZones.StoneLocations[i]);
                created++;
            }
        }

        return created;
    }

    private static bool HasPlot(string identifier, Map map)
    {
        foreach (var plot in _plots)
        {
            if (plot.Identifier == identifier && plot.Map == map)
            {
                return true;
            }
        }

        return false;
    }

    public static void StartTimer()
    {
        _tickToken.Cancel();
        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), ProcessTick, out _tickToken);
    }

    public static void EndTimer() => _tickToken.Cancel();

    private static void ProcessTick()
    {
        var plots = new List<MaginciaHousingPlot>(_plots);

        foreach (var plot in plots)
        {
            if (plot.IsAvailable && plot.LottoEnds != DateTime.MinValue && Core.Now > plot.LottoEnds)
            {
                plot.EndLotto();
            }

            if (plot.Expires != DateTime.MinValue && plot.Expires < Core.Now)
            {
                if (plot.Writ != null)
                {
                    plot.Writ.OnExpired();
                }
                else
                {
                    UnregisterPlot(plot);
                }
            }
        }

        if (_plots.Count == 0)
        {
            EndTimer();
        }
    }

    public static void RegisterPlot(MaginciaHousingPlot plot)
    {
        if (plot != null && !_plots.Contains(plot))
        {
            _plots.Add(plot);
        }
    }

    public static void UnregisterPlot(MaginciaHousingPlot plot)
    {
        if (plot == null)
        {
            return;
        }

        if (plot.Stone != null && !plot.Stone.Deleted)
        {
            plot.Stone.Plot = null;
            plot.Stone.Delete();
        }

        _plots.Remove(plot);

        if (plot.Map != null && _freeHousingZones.TryGetValue(plot.Map, out var zones) && !zones.Contains(plot.Bounds))
        {
            zones.Add(plot.Bounds);
        }
    }

    public static bool IsRegisteredPlot(MaginciaHousingPlot plot) => plot != null && _plots.Contains(plot);

    public static bool IsFreeHousingZone(Point3D p, Map map)
    {
        if (map == null || !_freeHousingZones.TryGetValue(map, out var zones))
        {
            return false;
        }

        foreach (var zone in zones)
        {
            if (zone.Contains(p))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Вызывается после постановки дома: жетон аренды на этом участке больше не нужен.</summary>
    public static void CheckHousePlacement(Mobile from, Point3D center)
    {
        if (!Enabled || from?.Backpack == null || !IsInMagincia(center.X, center.Y, from.Map))
        {
            return;
        }

        foreach (var lease in FindLeases(from))
        {
            if (lease is { Expired: false, Plot: not null } && lease.Plot.Bounds.Contains(center) &&
                from.Map == lease.Plot.Map)
            {
                lease.OnExpired();
                return;
            }
        }
    }

    private static List<WritOfLease> FindLeases(Mobile from)
    {
        var leases = new List<WritOfLease>();

        foreach (var lease in from.Backpack.FindItemsByType<WritOfLease>())
        {
            leases.Add(lease);
        }

        var bank = from.FindBankNoCreate();

        if (bank != null)
        {
            foreach (var lease in bank.FindItemsByType<WritOfLease>())
            {
                leases.Add(lease);
            }
        }

        return leases;
    }

    public static bool IsInMagincia(int x, int y, Map map) =>
        x > 3614 && x < 3817 && y > 2031 && y < 2274 && map is not null && (map == Map.Trammel || map == Map.Felucca);

    public static Point3D GetPlotStoneLoc(MaginciaHousingPlot plot)
    {
        if (plot == null)
        {
            return Point3D.Zero;
        }

        for (var i = 0; i < MaginciaHousingZones.Identifiers.Length; i++)
        {
            if (MaginciaHousingZones.Identifiers[i] == plot.Identifier)
            {
                return MaginciaHousingZones.StoneLocations[i];
            }
        }

        var z = plot.Map.GetAverageZ(plot.Bounds.X - 1, plot.Bounds.Y - 1);
        return new Point3D(plot.Bounds.X - 1, plot.Bounds.Y - 1, z);
    }

    #region Messages

    public static void SendMessageTo(Mobile from, TextDefinition title, TextDefinition body, TimeSpan expires) =>
        SendMessageTo(from, new NewMaginciaMessage(title, body, expires));

    public static void SendMessageTo(Mobile from, NewMaginciaMessage message)
    {
        if (from == null || message == null)
        {
            return;
        }

        AddMessageToQueue(from, message);

        if (from.NetState == null)
        {
            return;
        }

        from.CloseGump<NewMaginciaMessageGump>();
        NewMaginciaMessageGump.DisplayTo(from);
    }

    public static void AddMessageToQueue(Mobile from, NewMaginciaMessage message)
    {
        if (!_messageQueue.TryGetValue(from, out var messages) || messages == null)
        {
            messages = new List<NewMaginciaMessage>();
            _messageQueue[from] = messages;
        }

        messages.Add(message);
    }

    public static void RemoveMessageFromQueue(Mobile from, NewMaginciaMessage message)
    {
        if (from == null || message == null || !_messageQueue.TryGetValue(from, out var messages))
        {
            return;
        }

        messages.Remove(message);

        if (messages.Count == 0)
        {
            _messageQueue.Remove(from);
        }
    }

    public static bool HasMessageInQueue(Mobile from) =>
        from != null && _messageQueue.TryGetValue(from, out var messages) && messages is { Count: > 0 };

    public static List<NewMaginciaMessage> GetMessages(Mobile from) =>
        _messageQueue.TryGetValue(from, out var messages) ? messages : null;

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile from)
    {
        CheckMessages(from);

        if (from.NetState == null || !HasMessageInQueue(from))
        {
            _messageQueue.Remove(from);
            return;
        }

        from.CloseGump<NewMaginciaMessageGump>();
        NewMaginciaMessageGump.DisplayTo(from);

        GetWinnerGump(from);
    }

    public static void CheckMessages()
    {
        var expired = new List<Mobile>();

        foreach (var (mobile, messages) in _messageQueue)
        {
            messages?.RemoveAll(message => message.Expired);

            if (messages == null || messages.Count == 0)
            {
                expired.Add(mobile);
            }
        }

        foreach (var mobile in expired)
        {
            _messageQueue.Remove(mobile);
        }
    }

    public static void CheckMessages(Mobile from)
    {
        if (!_messageQueue.TryGetValue(from, out var messages) || messages == null)
        {
            return;
        }

        messages.RemoveAll(message => message.Expired);
    }

    public static void GetWinnerGump(Mobile from)
    {
        if (from?.Account is not Account account)
        {
            return;
        }

        for (var i = 0; i < account.Length; i++)
        {
            var mobile = account[i];

            if (mobile == null)
            {
                continue;
            }

            foreach (var plot in _plots)
            {
                if (plot.Expires != DateTime.MinValue && plot.Winner == mobile)
                {
                    PlotWinnerGump.DisplayTo(from, plot);
                    return;
                }
            }
        }
    }

    #endregion

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m) => _messageQueue.Remove(m);

    public static void SerializeState(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(GoldSink);
        writer.Write(Enabled);
        writer.Write(LottoDuration);

        writer.WriteEncodedInt(_plots.Count);

        foreach (var plot in _plots)
        {
            plot.Serialize(writer);
        }

        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            var zones = _freeHousingZones[map];
            writer.WriteEncodedInt(zones.Count);

            foreach (var zone in zones)
            {
                writer.Write(zone);
            }
        }

        writer.WriteEncodedInt(_messageQueue.Count);

        foreach (var (mobile, messages) in _messageQueue)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt(messages.Count);

            foreach (var message in messages)
            {
                message.Serialize(writer);
            }
        }
    }

    public static void DeserializeState(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        GoldSink = reader.ReadInt();
        Enabled = reader.ReadBool();
        LottoDuration = reader.ReadTimeSpan();

        _plots.Clear();
        _freeHousingZones[Map.Trammel].Clear();
        _freeHousingZones[Map.Felucca].Clear();
        _messageQueue.Clear();

        var plotCount = reader.ReadEncodedInt();

        for (var i = 0; i < plotCount; i++)
        {
            _plots.Add(MaginciaHousingPlot.Deserialize(reader));
        }

        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            var zoneCount = reader.ReadEncodedInt();

            for (var i = 0; i < zoneCount; i++)
            {
                _freeHousingZones[map].Add(reader.ReadRect2D());
            }
        }

        var queueCount = reader.ReadEncodedInt();

        for (var i = 0; i < queueCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var count = reader.ReadEncodedInt();
            var messages = new List<NewMaginciaMessage>(count);

            for (var j = 0; j < count; j++)
            {
                messages.Add(NewMaginciaMessage.Deserialize(reader));
            }

            if (mobile != null && messages.Count > 0)
            {
                _messageQueue[mobile] = messages;
            }
        }
    }
}
