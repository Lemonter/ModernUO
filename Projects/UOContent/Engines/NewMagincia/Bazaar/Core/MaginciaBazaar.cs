/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Core/MaginciaBizaar.cs).
///
///     Базар Новой Маджинции: сто прилавков, их аукционы аренды, «ответные» ставки арендаторов, ставки на
///     первый свободный прилавок и склад невостребованного имущества.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             В ServUO это был предмет-контроллер (<c>MaginciaBazaar : Item</c>), который сам себя
///             сериализовал и создавался командой <c>[GenNewMagincia</c>. Здесь система статическая, состояние
///             пишет <see cref="BazaarPersistence" />, а мир наполняется идемпотентно после
///             <c>ServerStarted</c> — по образцу <c>Engines/NewMagincia/MaginciaPlants/MaginciaPlantSystem.cs</c>.
///             Команд больше нет.
///         </item>
///         <item>
///             <c>[CommandProperty]</c> на предмете-контроллере больше нет; <see cref="Enabled" /> и
///             <see cref="PlotPhase" /> доступны статически, а состояние прилавков видно на вывесках.
///         </item>
///         <item>
///             <c>Timer.DelayCall(TimeSpan, TimeSpan, TimerCallback)</c> → <c>Timer.StartTimer</c> с токеном,
///             который отменяется вместе с системой.
///         </item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />; <c>ColUtility.Free</c> в этом репозитории нет.</item>
///         <item>
///             <c>AddToReserve</c>/<c>DeductReserve</c>/<c>GetBidMatching</c> в ServUO меняли словарь прямо в
///             <c>foreach</c> по ключам: <c>DeductReserve</c> при нулевом остатке удалял запись и ронял
///             перечисление. Здесь сначала ищется запись, и удаление идёт уже после выхода из цикла.
///         </item>
///         <item>
///             Просроченная запись склада, из которой всё уже разошлось (нет товаров, питомцев и денег),
///             удаляется — иначе словарь рос бы на одну запись за каждого потерявшего прилавок игрока навсегда.
///         </item>
///         <item>
///             В ServUO питомец, отправленный со склада в стойла, оставался в списке склада: на следующем тике
///             его пытались отправить снова, и если стойла уже были полны — удаляли питомца, который на самом
///             деле стоял у игрока в стойлах. Здесь питомец снимается со склада сразу после передачи.
///         </item>
///         <item>
///             <c>from.Stabled</c> в ModernUO есть только у <see cref="Server.Mobiles.PlayerMobile" />, поэтому
///             проверки вместимости стойл идут через него.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

public static class MaginciaBazaar
{
    /// <summary>Этапы открытия прилавков: A–D (0–39), E–G (40–69), H–J (70–99).</summary>
    public enum Phase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3
    }

    public static readonly int DefaultCommissionFee = 5;

    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(MaginciaBazaar));

    private static readonly List<MaginciaBazaarPlot> _plots = new();
    private static readonly Dictionary<Mobile, BidEntry> _nextAvailable = new();
    private static readonly Dictionary<Mobile, int> _reserve = new();
    private static readonly Dictionary<Mobile, StorageEntry> _warehouseStorage = new();

    /// <summary>Точки установки супертенданта склада (Trammel и Felucca).</summary>
    private static readonly Point3D SuperintendentLocation = new(3795, 2259, 20);

    private static BazaarPersistence _persistence;
    private static TimerExecutionToken _tickToken;
    private static Phase _phase = Phase.Phase1;
    private static bool _enabled = true;

    /// <summary>Выключение останавливает минутный тик, включение — запускает его, как в ServUO.</summary>
    public static bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;

            if (value)
            {
                StartTimer();
            }
            else
            {
                EndTimer();
            }
        }
    }

    public static Phase PlotPhase
    {
        get => _phase;
        set
        {
            if (value <= _phase)
            {
                return;
            }

            _phase = value;
            ActivatePlots();
        }
    }

    public static List<MaginciaBazaarPlot> Plots => _plots;

    public static Dictionary<Mobile, BidEntry> NextAvailableBids => _nextAvailable;

    public static TimeSpan GetShortAuctionTime => TimeSpan.FromMinutes(Utility.RandomMinMax(690, 750));

    public static TimeSpan GetLongAuctionTime => TimeSpan.FromHours(Utility.RandomMinMax(168, 180));

    public static void Configure() => _persistence = new BazaarPersistence();

    public static void Initialize() => EventSink.ServerStarted += OnServerStarted;

    private static void OnServerStarted() => Timer.StartTimer(TimeSpan.FromSeconds(30), EnsureSetup);

    /// <summary>
    ///     Идемпотентное наполнение мира: прилавки, их вывески со столбами и супертендант склада ставятся
    ///     только если их ещё нет. Загруженное состояние уже содержит прилавки, поэтому повторно ничего не
    ///     создаётся.
    /// </summary>
    private static void EnsureSetup()
    {
        try
        {
            var created = 0;

            if (_plots.Count == 0)
            {
                LoadPlots();
                created += _plots.Count;
            }

            created += AddMissingPlotSigns();
            created += EnsureSuperintendent(Map.Trammel);
            created += EnsureSuperintendent(Map.Felucca);

            if (created > 0)
            {
                _logger.Information("New Magincia bazaar is set up ({Count} objects placed)", created);
            }

            if (Enabled)
            {
                StartTimer();
            }
            else
            {
                EndTimer();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "New Magincia bazaar setup failed");
        }
    }

    private static int EnsureSuperintendent(Map map)
    {
        foreach (var mobile in map.GetMobilesInRange<WarehouseSuperintendent>(SuperintendentLocation, 2))
        {
            if (!mobile.Deleted)
            {
                return 0;
            }
        }

        var superintendent = new WarehouseSuperintendent
        {
            Home = SuperintendentLocation,
            RangeHome = 12
        };

        superintendent.MoveToWorld(SuperintendentLocation, map);
        return 1;
    }

    private static int AddMissingPlotSigns()
    {
        var created = 0;

        foreach (var plot in _plots)
        {
            if (plot.Sign != null)
            {
                continue;
            }

            var definition = plot.PlotDef;

            var pole = new Static(2592);
            pole.MoveToWorld(new Point3D(definition.SignLoc.X - 1, definition.SignLoc.Y, definition.SignLoc.Z), definition.Map);

            pole = new Static(2969);
            pole.MoveToWorld(definition.SignLoc, definition.Map);

            plot.AddPlotSign();
            created += 3;
        }

        return created;
    }

    private static void StartTimer()
    {
        EndTimer();
        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), OnTick, out _tickToken);
    }

    private static void EndTimer() => _tickToken.Cancel();

    public static bool IsActivePlot(MaginciaBazaarPlot plot)
    {
        var index = _plots.IndexOf(plot);

        if (index == -1)
        {
            return false;
        }

        return (int)_phase switch
        {
            1 => index < 40,
            2 => index < 70,
            3 => index < 100,
            _ => false
        };
    }

    public static void ActivatePlots()
    {
        for (var i = 0; i < _plots.Count; i++)
        {
            var plot = _plots[i];

            var active = (int)_phase switch
            {
                1 => i < 40,
                2 => i < 70,
                3 => i < 100,
                _ => false
            };

            if (active && plot.Auction == null)
            {
                plot.NewAuction(GetShortAuctionTime);
            }

            plot.Sign?.InvalidateProperties();
        }
    }

    private static void OnTick()
    {
        foreach (var plot in _plots)
        {
            if (plot.Active)
            {
                plot.OnTick();
            }
        }

        List<Mobile> toRemove = null;

        foreach (var (mobile, entry) in _warehouseStorage)
        {
            if (entry.Expires >= Core.Now)
            {
                continue;
            }

            var deleted = entry.CommodityTypes.Count > 0;
            var stabled = false;

            if (entry.Creatures.Count > 0)
            {
                var creatures = new List<BaseCreature>(entry.Creatures);

                foreach (var creature in creatures)
                {
                    if (creature == null || creature.Deleted)
                    {
                        entry.RemovePet(creature);
                        deleted = true;
                        continue;
                    }

                    if (mobile is PlayerMobile pm
                        && (pm.Stabled?.Count ?? 0) < AnimalTrainer.GetMaxStabled(mobile))
                    {
                        PetBroker.SendToStables(mobile, creature);
                        entry.RemovePet(creature);
                        stabled = true;
                    }
                    else
                    {
                        creature.Delete();
                        entry.RemovePet(creature);
                        deleted = true;
                    }
                }
            }

            if (stabled)
            {
                var message = deleted
                    ? "Ваши товары и/или деньги на складе Новой Маджинции переданы на благотворительность, " +
                      "потому что их не забрали за целую неделю. Вернуть их уже нельзя, зато сироты будут рады " +
                      "горячему обеду. Часть ваших питомцев отправлена в стойла."
                    : "Раз ваши питомцы простояли на складе больше недели, они отправлены в стойла. " +
                      "Если в стойлах не хватило места, остальные питомцы потеряны и отпущены на волю.";

                MaginciaLottoSystem.SendMessageTo(mobile, new NewMaginciaMessage(TextDefinition.Of(1150676), message));
            }
            else if (deleted)
            {
                toRemove ??= new List<Mobile>();
                toRemove.Add(mobile);

                // Ваши товары и/или деньги на складе Новой Маджинции переданы на благотворительность...
                MaginciaLottoSystem.SendMessageTo(
                    mobile,
                    new NewMaginciaMessage(TextDefinition.Of(1150676), TextDefinition.Of(1150673))
                );
            }
            else if (entry.CommodityTypes.Count == 0 && entry.Creatures.Count == 0 && entry.Funds <= 0)
            {
                toRemove ??= new List<Mobile>();
                toRemove.Add(mobile);
            }
        }

        if (toRemove == null)
        {
            return;
        }

        foreach (var mobile in toRemove)
        {
            _warehouseStorage.Remove(mobile);
        }
    }

    public static MaginciaBazaarPlot GetPlot(Mobile from)
    {
        foreach (var plot in _plots)
        {
            if (plot.IsOwner(from))
            {
                return plot;
            }
        }

        return null;
    }

    public static bool HasPlot(Mobile from) => GetPlot(from) != null;

    public static MaginciaBazaarPlot GetBiddingPlot(Mobile from)
    {
        if (from?.Account is not Account account)
        {
            return null;
        }

        for (var i = 0; i < account.Length; i++)
        {
            var mobile = account[i];

            if (mobile == null)
            {
                continue;
            }

            var plot = GetBiddingPlotForAccount(mobile);

            if (plot != null)
            {
                return plot;
            }
        }

        return null;
    }

    public static MaginciaBazaarPlot GetBiddingPlotForAccount(Mobile from)
    {
        foreach (var plot in _plots)
        {
            if (plot.Auction != null && plot.Auction.Auctioners.ContainsKey(from))
            {
                return plot;
            }
        }

        return null;
    }

    public static bool HasActiveBid(Mobile from) => GetBiddingPlot(from) != null || _nextAvailable.ContainsKey(from);

    public static bool TryRetractBid(Mobile from)
    {
        var plot = GetBiddingPlot(from);

        return plot != null ? plot.Auction.RetractBid(from) : RetractBid(from);
    }

    public static bool RetractBid(Mobile from)
    {
        if (from?.Account is not Account account)
        {
            return false;
        }

        for (var i = 0; i < account.Length; i++)
        {
            var mobile = account[i];

            if (mobile == null)
            {
                continue;
            }

            if (_nextAvailable.TryGetValue(mobile, out var entry) && entry != null && Banker.Deposit(mobile, entry.Amount))
            {
                _nextAvailable.Remove(mobile);
                return true;
            }
        }

        return false;
    }

    public static bool IsBiddingNextAvailable(Mobile from) => from != null && _nextAvailable.ContainsKey(from);

    public static int GetNextAvailableBid(Mobile from) =>
        from != null && _nextAvailable.TryGetValue(from, out var entry) ? entry.Amount : 0;

    public static void MakeBidNextAvailable(Mobile from, int amount) =>
        _nextAvailable[from] = new BidEntry(from, amount, BidType.NextAvailable);

    public static void RemoveBidNextAvailable(Mobile from) => _nextAvailable.Remove(from);

    public static void AwardPlot(MaginciaPlotAuction auction, Mobile winner, int highest)
    {
        var plot = auction.Plot;

        if (plot == null)
        {
            return;
        }

        _nextAvailable.Remove(winner);

        if (plot.Owner == winner)
        {
            if (plot.Owner != null)
            {
                plot.NewAuction(GetLongAuctionTime);
            }
            else
            {
                plot.Reset();
                plot.NewAuction(GetShortAuctionTime);
            }

            return;
        }

        var current = GetPlot(winner);

        if (current == null)
        {
            // Вы выиграли аренду прилавка ~1_STALLNAME~ на ~2_FACET~ базаре Новой Маджинции...
            MaginciaLottoSystem.SendMessageTo(
                winner,
                new NewMaginciaMessage(
                    null,
                    TextDefinition.Of(1150426),
                    $"{plot.PlotDef.ID}\t{plot.PlotDef.Map}\t{highest:N0}"
                )
            );
        }

        plot.Reset();

        if (current != null)
        {
            // Вы выиграли аренду и переехали на прилавок ~1_STALLNAME~...
            MaginciaLottoSystem.SendMessageTo(
                winner,
                new NewMaginciaMessage(
                    null,
                    TextDefinition.Of(1150428),
                    $"{plot.PlotDef.ID}\t{plot.PlotDef.Map}\t{highest:N0}"
                )
            );

            plot.PlotMulti = current.PlotMulti;
            plot.Merchant = current.Merchant;
            plot.ShopName = current.ShopName;

            current.PlotMulti = null;
            current.Merchant = null;
            current.Owner = null;

            current.Auction?.EndAuction();
        }

        plot.Owner = winner;
        plot.NewAuction(GetLongAuctionTime);
    }

    public static void RegisterPlot(PlotDef plotDef) => _plots.Add(new MaginciaBazaarPlot(plotDef));

    public static bool IsSameAccount(Mobile check, Mobile checkAgainst) =>
        check != null && checkAgainst != null && check.Account != null && check.Account == checkAgainst.Account;

    #region Склад Новой Маджинции

    public static void AddInventoryToWarehouse(Mobile owner, BaseBazaarBroker broker)
    {
        if (owner == null || broker == null)
        {
            return;
        }

        var entry = GetStorageEntry(owner);

        if (entry == null)
        {
            if (broker.HasValidEntry(owner))
            {
                entry = new StorageEntry(owner, broker);
            }
        }
        else if (broker.HasValidEntry(owner))
        {
            entry.AddInventory(owner, broker);
        }

        if (entry == null)
        {
            return;
        }

        _warehouseStorage[owner] = entry;

        // Ваш маклер перевёл остатки имущества и денег с прилавка на склад Новой Маджинции...
        MaginciaLottoSystem.SendMessageTo(
            owner,
            new NewMaginciaMessage(TextDefinition.Of(1150676), TextDefinition.Of(1150674))
        );
    }

    public static StorageEntry GetStorageEntry(Mobile from) =>
        from != null && _warehouseStorage.TryGetValue(from, out var entry) ? entry : null;

    public static void RemoveFromStorage(Mobile from)
    {
        if (from != null)
        {
            _warehouseStorage.Remove(from);
        }
    }

    #endregion

    #region Ответная ставка (match bid)

    public static void AddToReserve(Mobile from, int amount)
    {
        if (from == null)
        {
            return;
        }

        if (_reserve.TryGetValue(from, out var current))
        {
            _reserve[from] = current + amount;
            return;
        }

        foreach (var (mobile, value) in _reserve)
        {
            if (IsSameAccount(from, mobile))
            {
                _reserve[mobile] = value + amount;
                return;
            }
        }

        _reserve[from] = amount;
    }

    public static void DeductReserve(Mobile from, int amount)
    {
        if (from == null)
        {
            return;
        }

        Mobile matched = null;
        var left = 0;

        if (_reserve.TryGetValue(from, out var own))
        {
            matched = from;
            left = own - amount;
        }
        else
        {
            foreach (var (mobile, value) in _reserve)
            {
                if (IsSameAccount(from, mobile))
                {
                    matched = mobile;
                    left = value - amount;
                    break;
                }
            }
        }

        if (matched == null)
        {
            return;
        }

        if (left <= 0)
        {
            _reserve.Remove(matched);
        }
        else
        {
            _reserve[matched] = left;
        }
    }

    public static bool HasReserve(Mobile from) => GetBidMatching(from) > 0;

    public static int GetBidMatching(Mobile from)
    {
        if (from == null)
        {
            return 0;
        }

        if (_reserve.TryGetValue(from, out var own))
        {
            return own;
        }

        foreach (var (mobile, value) in _reserve)
        {
            if (IsSameAccount(from, mobile))
            {
                return value;
            }
        }

        return 0;
    }

    #endregion

    public static void LoadPlots()
    {
        var idx = 0;
        RegisterStall("A-1", idx, 0);
        RegisterStall("A-1", idx, 1);
        idx++;
        RegisterStall("A-2", idx, 0);
        RegisterStall("A-2", idx, 1);
        idx++;
        RegisterStall("A-3", idx, 0);
        RegisterStall("A-3", idx, 1);
        idx++;
        RegisterStall("A-4", idx, 0);
        RegisterStall("A-4", idx, 1);
        idx++;
        RegisterStall("B-1", idx, 0);
        RegisterStall("B-1", idx, 1);
        idx++;
        RegisterStall("B-2", idx, 0);
        RegisterStall("B-2", idx, 1);
        idx++;
        RegisterStall("B-3", idx, 0);
        RegisterStall("B-3", idx, 1);
        idx++;
        RegisterStall("B-4", idx, 0);
        RegisterStall("B-4", idx, 1);
        idx++;
        RegisterStall("B-5", idx, 0);
        RegisterStall("B-5", idx, 1);
        idx++;
        RegisterStall("B-6", idx, 1);
        RegisterStall("B-6", idx, 0);
        idx++;
        RegisterStall("C-1", idx, 1);
        RegisterStall("C-1", idx, 0);
        idx++;
        RegisterStall("C-2", idx, 1);
        RegisterStall("C-2", idx, 0);
        idx++;
        RegisterStall("C-3", idx, 1);
        RegisterStall("C-3", idx, 0);
        idx++;
        RegisterStall("C-4", idx, 1);
        RegisterStall("C-4", idx, 0);
        idx++;
        RegisterStall("C-5", idx, 1);
        RegisterStall("C-5", idx, 0);
        idx++;
        RegisterStall("D-1", idx, 1);
        RegisterStall("D-1", idx, 0);
        idx++;
        RegisterStall("D-2", idx, 1);
        RegisterStall("D-2", idx, 0);
        idx++;
        RegisterStall("D-3", idx, 1);
        RegisterStall("D-3", idx, 0);
        idx++;
        RegisterStall("D-4", idx, 1);
        RegisterStall("D-4", idx, 0);
        idx++;
        RegisterStall("D-5", idx, 1);
        RegisterStall("D-5", idx, 0);
        idx++;
        RegisterStall("E-1", idx, 1);
        RegisterStall("E-1", idx, 0);
        idx++;
        RegisterStall("E-2", idx, 1);
        RegisterStall("E-2", idx, 0);
        idx++;
        RegisterStall("E-3", idx, 1);
        RegisterStall("E-3", idx, 0);
        idx++;
        RegisterStall("E-4", idx, 1);
        RegisterStall("E-4", idx, 0);
        idx++;
        RegisterStall("E-5", idx, 1);
        RegisterStall("E-5", idx, 0);
        idx++;
        RegisterStall("F-1", idx, 1);
        RegisterStall("F-1", idx, 0);
        idx++;
        RegisterStall("F-2", idx, 1);
        RegisterStall("F-2", idx, 0);
        idx++;
        RegisterStall("F-3", idx, 1);
        RegisterStall("F-3", idx, 0);
        idx++;
        RegisterStall("F-4", idx, 1);
        RegisterStall("F-4", idx, 0);
        idx++;
        RegisterStall("F-5", idx, 1);
        RegisterStall("F-5", idx, 0);
        idx++;
        RegisterStall("G-1", idx, 1);
        RegisterStall("G-1", idx, 0);
        idx++;
        RegisterStall("G-2", idx, 1);
        RegisterStall("G-2", idx, 0);
        idx++;
        RegisterStall("G-3", idx, 1);
        RegisterStall("G-3", idx, 0);
        idx++;
        RegisterStall("G-4", idx, 1);
        RegisterStall("G-4", idx, 0);
        idx++;
        RegisterStall("G-5", idx, 1);
        RegisterStall("G-5", idx, 0);
        idx++;
        RegisterStall("H-1", idx, 1);
        RegisterStall("H-1", idx, 0);
        idx++;
        RegisterStall("H-2", idx, 1);
        RegisterStall("H-2", idx, 0);
        idx++;
        RegisterStall("H-3", idx, 1);
        RegisterStall("H-3", idx, 0);
        idx++;
        RegisterStall("H-4", idx, 1);
        RegisterStall("H-4", idx, 0);
        idx++;
        RegisterStall("H-5", idx, 1);
        RegisterStall("H-5", idx, 0);
        idx++;
        RegisterStall("H-6", idx, 1);
        RegisterStall("H-6", idx, 0);
        idx++;
        RegisterStall("I-1", idx, 1);
        RegisterStall("I-1", idx, 0);
        idx++;
        RegisterStall("I-2", idx, 1);
        RegisterStall("I-2", idx, 0);
        idx++;
        RegisterStall("I-3", idx, 1);
        RegisterStall("I-3", idx, 0);
        idx++;
        RegisterStall("I-4", idx, 1);
        RegisterStall("I-4", idx, 0);
        idx++;
        RegisterStall("I-5", idx, 1);
        RegisterStall("I-5", idx, 0);
        idx++;
        RegisterStall("J-1", idx, 1);
        RegisterStall("J-1", idx, 0);
        idx++;
        RegisterStall("J-2", idx, 1);
        RegisterStall("J-2", idx, 0);
        idx++;
        RegisterStall("J-3", idx, 1);
        RegisterStall("J-3", idx, 0);
        idx++;
        RegisterStall("J-4", idx, 1);
        RegisterStall("J-4", idx, 0);
    }

    private static void RegisterStall(string id, int index, int mapId) =>
        RegisterPlot(new PlotDef(id, StallLocations[index], mapId));

    /// <summary>Точки установки прилавков в порядке <see cref="LoadPlots" />.</summary>
    private static readonly Point3D[] StallLocations =
    {
        // A
        new(3716, 2198, 20),
        new(3709, 2198, 20),
        new(3700, 2192, 20),
        new(3693, 2192, 20),

        // B
        new(3686, 2192, 20),
        new(3686, 2198, 20),
        new(3686, 2204, 20),
        new(3686, 2210, 20),
        new(3686, 2216, 20),
        new(3686, 2222, 20),

        // C
        new(3686, 2228, 20),
        new(3692, 2228, 20),
        new(3698, 2228, 20),
        new(3704, 2228, 20),
        new(3710, 2228, 20),

        // D
        new(3716, 2228, 20),
        new(3716, 2222, 20),
        new(3716, 2216, 20),
        new(3716, 2210, 20),
        new(3716, 2204, 20),

        // E
        new(3686, 2178, 20),
        new(3686, 2171, 20),
        new(3686, 2164, 20),
        new(3686, 2157, 20),
        new(3686, 2150, 20),

        // F
        new(3693, 2178, 20),
        new(3693, 2171, 20),
        new(3693, 2164, 20),
        new(3693, 2157, 20),
        new(3693, 2150, 20),

        // G
        new(3700, 2178, 20),
        new(3700, 2171, 20),
        new(3700, 2164, 20),
        new(3700, 2157, 20),
        new(3700, 2150, 20),

        // H
        new(3672, 2238, 20),
        new(3665, 2238, 20),
        new(3658, 2238, 20),
        new(3672, 2245, 20),
        new(3665, 2245, 20),
        new(3658, 2245, 20),

        // I
        new(3730, 2249, 20),
        new(3730, 2242, 20),
        new(3737, 2242, 20),
        new(3737, 2249, 20),
        new(3737, 2256, 20),

        // J
        new(3730, 2220, 20),
        new(3737, 2220, 20),
        new(3730, 2228, 20),
        new(3737, 2228, 20)
    };

    public static void SerializeState(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(Enabled);
        writer.Write((int)_phase);

        writer.WriteEncodedInt(_plots.Count);

        foreach (var plot in _plots)
        {
            plot.Serialize(writer);
        }

        writer.WriteEncodedInt(_nextAvailable.Count);

        foreach (var (mobile, entry) in _nextAvailable)
        {
            writer.Write(mobile);
            entry.Serialize(writer);
        }

        writer.WriteEncodedInt(_warehouseStorage.Count);

        foreach (var (mobile, entry) in _warehouseStorage)
        {
            writer.Write(mobile);
            entry.Serialize(writer);
        }

        writer.WriteEncodedInt(_reserve.Count);

        foreach (var (mobile, amount) in _reserve)
        {
            writer.Write(mobile);
            writer.Write(amount);
        }
    }

    public static void DeserializeState(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        // Поле, а не свойство: тик запускает EnsureSetup после ServerStarted, а не посреди загрузки мира.
        _enabled = reader.ReadBool();
        _phase = (Phase)reader.ReadInt();

        _plots.Clear();
        _nextAvailable.Clear();
        _warehouseStorage.Clear();
        _reserve.Clear();

        var plotCount = reader.ReadEncodedInt();

        for (var i = 0; i < plotCount; i++)
        {
            _plots.Add(MaginciaBazaarPlot.Deserialize(reader));
        }

        var bidCount = reader.ReadEncodedInt();

        for (var i = 0; i < bidCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var entry = BidEntry.Deserialize(reader);

            if (mobile != null)
            {
                _nextAvailable[mobile] = entry;
            }
        }

        var storageCount = reader.ReadEncodedInt();

        for (var i = 0; i < storageCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var entry = StorageEntry.Deserialize(reader);

            if (mobile != null)
            {
                _warehouseStorage[mobile] = entry;
            }
        }

        var reserveCount = reader.ReadEncodedInt();

        for (var i = 0; i < reserveCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var amount = reader.ReadInt();

            if (mobile != null && amount > 0)
            {
                _reserve[mobile] = amount;
            }
        }
    }
}
