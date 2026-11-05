/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Items/WritOfLease.cs).
///
///     Жетон аренды участка: выдаётся победителю лотереи, даёт право поставить дом на конкретном участке и
///     работает как руна возврата на его координаты.
///
///     Замены:
///     <list type="bullet">
///         <item><c>ObjectPropertyList</c> → <see cref="IPropertyList" />.</item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///         <item>
///             Ссылка на участок не сериализуется: её восстанавливает <see cref="MaginciaHousingPlot.Deserialize" />
///             из своего поля <c>Writ</c>, поэтому двойного владения нет.
///         </item>
///         <item>
///             Уведомление системы перенесено из <c>Delete()</c> в <see cref="OnAfterDelete" />; вложенное окно
///             осталось обычным <see cref="Gump" /> с явным <c>DisplayTo</c>, чтобы не открывать пустую рамку.
///         </item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class WritOfLease : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _expired;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _facet;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _identifier;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _recallLoc;

    [Constructible]
    public WritOfLease() : base(5358)
    {
        Hue = 0x9A;
        _expired = true;
        _expires = DateTime.MinValue;
    }

    public WritOfLease(MaginciaHousingPlot plot) : base(5358)
    {
        Hue = 0x9A;
        Plot = plot;
        _expires = plot?.Expires ?? DateTime.MinValue;

        if (plot != null)
        {
            _facet = plot.Map;
            _identifier = plot.Identifier;
            _recallLoc = plot.RecallLoc;
        }
    }

    public override int LabelNumber => 1150489; // a writ of lease

    public MaginciaHousingPlot Plot { get; set; }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // Lot: ~1_LOTNAME~
        list.Add(1150547, _identifier ?? "неизвестно");

        // Facet: Trammel / Facet: Felucca
        list.Add(_facet == Map.Trammel ? 1150549 : 1150548);

        list.Add(1150546, Server.Misc.ServerList.ServerName); // Shard: ~1_SHARDNAME~

        if (_expired)
        {
            list.Add(1150487); // [Expired]
        }
    }

    public override void OnDoubleClick(Mobile from) => WritNoteGump.DisplayTo(from, this);

    public void CheckExpired()
    {
        if (Core.Now > _expires)
        {
            OnExpired();
        }
    }

    public void OnExpired()
    {
        if (_expired)
        {
            return;
        }

        MaginciaLottoSystem.UnregisterPlot(Plot);
        Plot = null;
        _expired = true;
        _expires = DateTime.MinValue;
        InvalidateProperties();
    }

    public override void OnAfterDelete()
    {
        if (!_expired)
        {
            OnExpired();
        }

        Plot = null;
        base.OnAfterDelete();
    }

    private class WritNoteGump : Gump
    {
        public static void DisplayTo(Mobile from, WritOfLease lease)
        {
            if (from == null || lease?.Deleted != false)
            {
                return;
            }

            from.CloseGump<WritNoteGump>();
            from.SendGump(new WritNoteGump(lease));
        }

        private WritNoteGump(WritOfLease lease) : base(100, 100)
        {
            AddImage(0, 0, 9380);
            AddImage(114, 0, 9381);
            AddImage(171, 0, 9382);
            AddImage(0, 140, 9386);
            AddImage(114, 140, 9387);
            AddImage(171, 140, 9388);

            AddHtmlLocalized(90, 5, 200, 16, 1150484, 1, false, false); // WRIT OF LEASE

            var recall = $"{lease._recallLoc.X} {lease._recallLoc.Y}";

            if (lease._expired)
            {
                var args = $"{lease._identifier}\t{lease._facet}\t{Server.Misc.ServerList.ServerName}\t\t{recall}";
                AddHtmlLocalized(38, 55, 215, 178, 1150488, args, 1, false, true);
            }
            else
            {
                var args =
                    $"{lease._identifier}\t{lease._facet}\t{Server.Misc.ServerList.ServerName}\t{MaginciaLottoSystem.WritExpirePeriod}\t{recall}";
                AddHtmlLocalized(38, 55, 215, 178, 1150483, args, 1, false, true);
            }
        }
    }
}
