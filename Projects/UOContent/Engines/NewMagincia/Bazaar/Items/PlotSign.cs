/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Items/PlotSign.cs).
///
///     Вывеска прилавка: показывает название магазина, маклера, остаток времени аукциона и открывает окно
///     аренды. Из контекстного меню можно купить руну возврата к прилавку.
///
///     Замены:
///     <list type="bullet">
///         <item><c>ObjectPropertyList</c> → <see cref="IPropertyList" />, <c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///         <item>
///             <c>GetContextMenuEntries(Mobile, List&lt;ContextMenuEntry&gt;)</c> →
///             <c>AddCustomContextEntries(Mobile, ref PooledRefList&lt;ContextMenuEntry&gt;)</c>.
///         </item>
///         <item>
///             Участок (<see cref="Plot" />) не сериализуется: его восстанавливает сам прилавок из ссылки на
///             вывеску. Конструктора без параметров в ServUO не было, он добавлен для генератора и помечен
///             <c>[Constructible]</c>.
///         </item>
///         <item><c>String.Format</c> → интерполяция; тексты, которые видит игрок, переведены на русский.</item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class PlotSign : Item
{
    public static readonly int RuneCost = 100;

    private MaginciaBazaarPlot _plot;

    [Constructible]
    public PlotSign() : base(3025) => Movable = false;

    public PlotSign(MaginciaBazaarPlot plot) : base(3025)
    {
        Movable = false;
        _plot = plot;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public MaginciaBazaarPlot Plot
    {
        get => _plot;
        set
        {
            _plot = value;
            InvalidateProperties();
        }
    }

    public override bool DisplayWeight => false;

    public override void OnDoubleClick(Mobile from)
    {
        if (_plot == null)
        {
            from.SendMessage("Эта вывеска ни к чему не привязана.");
        }
        else if (!_plot.Active)
        {
            from.SendMessage($"Прилавок {_plot.PlotDef.ID} сейчас не сдаётся.");
        }
        else if (from.InRange(Location, 3))
        {
            StallLeasingGump.DisplayTo(from, _plot);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_plot == null)
        {
            return;
        }

        if (_plot.ShopName != null)
        {
            list.Add(1062449, _plot.ShopName); // Shop Name: ~1_NAME~
        }

        if (_plot.Merchant != null)
        {
            list.Add(1150529, _plot.Merchant.Name); // Proprietor: ~1_NAME~
        }

        if (_plot.Auction != null)
        {
            var left = 1;

            if (_plot.Auction.AuctionEnd > Core.Now)
            {
                var leftSpan = _plot.Auction.AuctionEnd - Core.Now;
                left = (int)(leftSpan.TotalHours + 1);
            }

            list.Add(1150533, $"{left}"); // Auction for Lease Ends Within ~1_HOURS~ Hours
        }

        if (!_plot.Active)
        {
            list.Add(1153036); // Inactive
        }
    }

    public override void AddNameProperty(IPropertyList list)
    {
        // Stall ~1_NAME~
        list.Add(1150530, _plot?.PlotDef?.ID ?? "неизвестно");
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        if (_plot != null && _plot.Active)
        {
            list.Add(new RecallRuneEntry(from, this));
        }

        base.GetContextMenuEntries(from, ref list);
    }

    private class RecallRuneEntry : ContextMenuEntry
    {
        private readonly PlotSign _sign;

        public RecallRuneEntry(Mobile from, PlotSign sign) : base(1151508, -1)
        {
            _sign = sign;
            Enabled = from.InRange(sign.Location, 2);
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from != null)
            {
                ShopRecallRuneGump.DisplayTo(from, _sign);
            }
        }
    }
}
