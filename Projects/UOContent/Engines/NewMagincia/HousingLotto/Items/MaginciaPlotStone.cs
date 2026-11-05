/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Items/MaginciaPlotStone.cs).
///
///     Камень-указатель участка: показывает обозначение лота и открывает окно лотереи.
///
///     Замены:
///     <list type="bullet">
///         <item><c>ObjectPropertyList</c> → <see cref="IPropertyList" />.</item>
///         <item>Ссылка на систему статическая — <c>MaginciaLottoSystem.Instance</c> больше не нужен.</item>
///         <item>Удаление камня снимает участок с регистрации в <see cref="OnAfterDelete" />.</item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class MaginciaPlotStone : Item
{
    [Constructible]
    public MaginciaPlotStone() : base(3805) => Movable = false;

    public override bool ForceShowProperties => true;

    public MaginciaHousingPlot Plot { get; set; }

    public override void AddNameProperty(IPropertyList list)
    {
        // lot ~1_PLOTID~
        list.Add(1150494, Plot?.Identifier ?? "неизвестно");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!MaginciaLottoSystem.Enabled || Plot == null || !from.InRange(Location, 4))
        {
            return;
        }

        if (Plot.LottoOngoing)
        {
            MaginciaLottoGump.DisplayTo(from, Plot);
        }
        else if (!Plot.IsAvailable)
        {
            from.SendMessage("Лотерея этого участка завершена.");
        }
        else
        {
            from.SendMessage("Лотерея этого участка ещё не началась. Загляните позже!");
        }
    }

    public override void OnAfterDelete()
    {
        if (Plot != null)
        {
            var plot = Plot;
            Plot = null;
            MaginciaLottoSystem.UnregisterPlot(plot);
        }

        base.OnAfterDelete();
    }
}
