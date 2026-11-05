/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Core/MaginciaBizaarPlot.cs).
///
///     Прилавок базара: описание места, арендатор, вывеска, мульти-палатка и маклер. Пишется
///     <see cref="BazaarPersistence" /> через <see cref="MaginciaBazaar.SerializeState" />.
///
///     Замены:
///     <list type="bullet">
///         <item><c>[PropertyObject]</c> в этом репозитории нет — атрибут снят.</item>
///         <item><c>Timer.DelayCall</c> → <c>Timer.StartTimer</c>; временный мульти отменяется токеном.</item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///         <item>
///             <c>BaseGuildGump.CheckProfanity</c> вызывается с ограничением длины 40 — в ModernUO у него есть
///             параметр <c>maxLength</c>, поэтому отдельная проверка длины оставлена только для сообщения.
///         </item>
///         <item>
///             Геттер <c>Auction</c> в ServUO содержал закомментированное создание аукциона «по требованию» —
///             удалено: аукцион создаётся только явно через <see cref="NewAuction" />, иначе после загрузки
///             мира появились бы аукционы у неактивных прилавков.
///         </item>
///         <item>
///             <c>Reset()</c> в ServUO откладывал удаление палатки на две минуты и удалял в отложенном вызове
///             <em>текущий</em> мульти прилавка. При переезде арендатора на новый прилавок туда переносилась
///             палатка старого, и через две минуты отложенный вызов уничтожал уже новую. Здесь удаляется
///             именно та палатка, которую снимали.
///         </item>
///         <item>
///             Вывеска, маклер и палатка при десериализации восстанавливаются полями, а не свойствами: сеттеры
///             передвигают сущности в мир, а после загрузки они уже стоят там, где были сохранены.
///         </item>
///     </list>
/// </summary>
using System;
using Server.Guilds;

namespace Server.Engines.NewMagincia;

public class MaginciaBazaarPlot
{
    private TimerExecutionToken _tempMultiToken;
    private BaseBazaarMulti _plotMulti;
    private BaseBazaarBroker _merchant;

    public MaginciaBazaarPlot(PlotDef definition)
    {
        PlotDef = definition;
        Owner = null;
        _plotMulti = null;
        _merchant = null;
        ShopName = null;
    }

    private MaginciaBazaarPlot()
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public PlotDef PlotDef { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Owner { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string ShopName { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public BaseBazaarMulti PlotMulti
    {
        get => _plotMulti;
        set
        {
            if (_plotMulti != null && _plotMulti != value && value != null)
            {
                _plotMulti.Delete();
                _plotMulti = null;
            }

            _plotMulti = value;

            if (_plotMulti != null)
            {
                _plotMulti.MoveToWorld(PlotDef.MultiLocation, PlotDef.Map);
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public BaseBazaarBroker Merchant
    {
        get => _merchant;
        set
        {
            _merchant = value;

            if (_merchant == null)
            {
                return;
            }

            _merchant.Plot = this;

            var location = PlotDef.Location;
            location.X++;
            location.Y++;
            location.Z = 27;

            var fillers = _plotMulti?.Fillers;

            if (fillers is { Count: > 0 })
            {
                var filler = fillers[0];
                location = filler.Location;
                location.Z = filler.Z + TileData.ItemTable[filler.ItemID & TileData.MaxItemValue].CalcHeight;
            }

            _merchant.MoveToWorld(location, PlotDef.Map);
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public PlotSign Sign { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public MaginciaPlotAuction Auction { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active => MaginciaBazaar.IsActivePlot(this);

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime AuctionEnds => Auction?.AuctionEnd ?? DateTime.MinValue;

    public override string ToString() => PlotDef?.ID ?? "?";

    public bool IsOwner(Mobile from)
    {
        if (from == null || Owner == null)
        {
            return false;
        }

        if (from == Owner)
        {
            return true;
        }

        return from.Account != null && from.Account == Owner.Account;
    }

    public void AddPlotSign()
    {
        Sign = new PlotSign(this);
        Sign.MoveToWorld(PlotDef.SignLoc, PlotDef.Map);
    }

    public void Reset()
    {
        if (_plotMulti != null)
        {
            // Палатка снимается через две минуты, но ссылка на неё отпускается сразу: иначе отложенный вызов
            // удалил бы палатку, которую к тому времени уже передали новому прилавку при переезде арендатора.
            var multi = _plotMulti;
            _plotMulti = null;

            Timer.StartTimer(TimeSpan.FromMinutes(2), () => multi.Delete());
        }

        EndTempMultiTimer();

        Merchant?.Dismiss();

        Owner = null;
        ShopName = null;
        Merchant = null;
    }

    public void NewAuction(TimeSpan time)
    {
        Auction = new MaginciaPlotAuction(this, time);
        Sign?.InvalidateProperties();
    }

    public void OnTick()
    {
        Auction?.OnTick();
        Merchant?.OnTick();
        Sign?.InvalidateProperties();
    }

    #region Временные мульти-палатки

    public void AddTempMulti(int idx1, int idx2)
    {
        if (PlotMulti != null)
        {
            PlotMulti.Delete();
            PlotMulti = null;
        }

        BaseBazaarMulti multi = idx1 == 0
            ? idx2 switch
            {
                0 => new CommodityStyle1(),
                1 => new CommodityStyle2(),
                2 => new CommodityStyle3(),
                _ => null
            }
            : idx2 switch
            {
                0 => new PetStyle1(),
                1 => new PetStyle2(),
                2 => new PetStyle3(),
                _ => null
            };

        if (multi == null)
        {
            return;
        }

        PlotMulti = multi;
        BeginTempMultiTimer();
    }

    public void ConfirmMulti(bool commodity)
    {
        EndTempMultiTimer();

        if (commodity)
        {
            Merchant = new CommodityBroker(this);
        }
        else
        {
            Merchant = new PetBroker(this);
        }
    }

    public void RemoveTempPlot()
    {
        EndTempMultiTimer();

        if (PlotMulti != null)
        {
            PlotMulti.Delete();
            PlotMulti = null;
        }
    }

    public void BeginTempMultiTimer()
    {
        EndTempMultiTimer();

        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), RemoveTempPlot, out _tempMultiToken);
    }

    public void EndTempMultiTimer() => _tempMultiToken.Cancel();

    public bool HasTempMulti() => _tempMultiToken.Running;

    #endregion

    public bool TrySetShopName(Mobile from, string text)
    {
        if (text == null || !BaseGuildGump.CheckProfanity(text, 40))
        {
            return false;
        }

        ShopName = text;
        Merchant?.InvalidateProperties();
        Sign?.InvalidateProperties();

        from.SendLocalizedMessage(1150333); // Your shop has been renamed.
        return true;
    }

    public void FireBroker()
    {
        if (Merchant == null)
        {
            return;
        }

        Merchant.Delete();
        Merchant = null;

        if (PlotMulti != null)
        {
            PlotMulti.Delete();
            PlotMulti = null;
        }
    }

    public void Abandon()
    {
        Reset();

        Auction?.ChangeAuctionTime(MaginciaBazaar.GetShortAuctionTime);
    }

    public int GetBid(Mobile from) => Auction?.GetBidAmount(from) ?? 0;

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        PlotDef.Serialize(writer);

        writer.Write(Owner);
        writer.Write(ShopName);
        writer.Write(Merchant);

        WriteMerchantEntries(writer);

        writer.Write(Sign);
        writer.Write(PlotMulti);

        if (Auction != null)
        {
            writer.Write(true);
            Auction.Serialize(writer);
        }
        else
        {
            writer.Write(false);
        }
    }

    private void WriteMerchantEntries(IGenericWriter writer)
    {
        switch (Merchant)
        {
            case CommodityBroker commodity:
                {
                    writer.Write((byte)1);
                    commodity.SerializeEntries(writer);
                    break;
                }
            case PetBroker pets:
                {
                    writer.Write((byte)2);
                    pets.SerializeEntries(writer);
                    break;
                }
            default:
                {
                    writer.Write((byte)0);
                    break;
                }
        }
    }

    public static MaginciaBazaarPlot Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var plot = new MaginciaBazaarPlot
        {
            PlotDef = PlotDef.Deserialize(reader),
            Owner = reader.ReadEntity<Mobile>(),
            ShopName = reader.ReadString()
        };

        // Маклер, вывеска и палатка восстанавливаются полями, а не свойствами: сеттеры передвигают сущности
        // в мир, а после загрузки они уже стоят там, где были сохранены.
        var merchant = reader.ReadEntity<BaseBazaarBroker>();

        switch (reader.ReadByte())
        {
            case 1:
                {
                    CommodityBroker.DeserializeEntries(reader, merchant as CommodityBroker);
                    break;
                }
            case 2:
                {
                    PetBroker.DeserializeEntries(reader, merchant as PetBroker);
                    break;
                }
        }

        plot._merchant = merchant;
        plot.Sign = reader.ReadEntity<PlotSign>();
        plot._plotMulti = reader.ReadEntity<BaseBazaarMulti>();

        if (reader.ReadBool())
        {
            plot.Auction = MaginciaPlotAuction.Deserialize(reader, plot);
        }

        if (merchant != null)
        {
            merchant.Plot = plot;
        }

        if (plot.Sign != null)
        {
            plot.Sign.Plot = plot;
        }

        return plot;
    }
}

/// <summary>Описание места прилавка: обозначение, точка установки и фасет.</summary>
public class PlotDef
{
    public PlotDef(string id, Point3D location, int mapId)
    {
        ID = id;
        Location = location;
        Map = Server.Map.Maps[mapId];
    }

    private PlotDef()
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public string ID { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D Location { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Map Map { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D SignLoc => new(Location.X + 1, Location.Y - 2, Location.Z);

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D MultiLocation => new(Location.X, Location.Y, Location.Z + 2);

    public override string ToString() => ID;

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(ID);
        writer.Write(Location);
        writer.Write(Map);
    }

    public static PlotDef Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        return new PlotDef
        {
            ID = reader.ReadString(),
            Location = reader.ReadPoint3D(),
            Map = reader.ReadMap()
        };
    }
}
