/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Gumps/ShipAccessListGump.cs, класс
///     <c>AccessListGump</c>).
///
///     Постраничный список тех, кому разрешён доступ на корабль, с их действующим уровнем. Клик по
///     игроку открывает <see cref="GrantAccessGump" />, где уровень можно изменить или игрока убрать.
///
///     Замены:
///     <list type="bullet">
///         <item>Разбивка на страницы переписана: в ServUO кнопки «вперёд/назад» ставились внутри
///             цикла строк (и на первой странице их не было вовсе); здесь страницы считаются заранее.</item>
///         <item>Подписи и уровни — русскими строками вместо cliloc-ов 1149725–1149777.</item>
///         <item>Удалённые персонажи и владелец в список не попадают.</item>
///     </list>
/// </summary>
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.HighSeas;

public class ShipAccessListGump : BaseShipGump
{
    private const int RowsPerPage = 10;
    private const int RowHeight = 25;
    private const int FirstRowY = 79;
    private const int EntryButtonBase = 100;

    private readonly BaseGalleon _galleon;
    private readonly List<Mobile> _entries;

    private ShipAccessListGump(BaseGalleon galleon) : base(galleon)
    {
        _galleon = galleon;
        _entries = CollectEntries(galleon);

        AddButton(10, 355, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
        AddLabel(45, 357, LabelColor, "ГЛАВНОЕ МЕНЮ");

        if (_entries.Count == 0)
        {
            AddLabel(45, FirstRowY, NoHue, "Список доступа пуст.");
            return;
        }

        var pages = (_entries.Count + RowsPerPage - 1) / RowsPerPage;

        for (var page = 0; page < pages; page++)
        {
            if (page > 0)
            {
                AddPage(page);
            }

            if (page > 0)
            {
                AddButton(10, 330, 4014, 4016, 0, GumpButtonType.Page, page - 1);
                AddLabel(45, 332, LabelColor, "Назад");
            }

            if (page < pages - 1)
            {
                AddButton(230, 330, 4005, 4007, 0, GumpButtonType.Page, page + 1);
                AddLabel(265, 332, LabelColor, "Далее");
            }

            var start = page * RowsPerPage;
            var y = FirstRowY;

            for (var i = start; i < start + RowsPerPage && i < _entries.Count; i++)
            {
                var mobile = _entries[i];
                var level = galleon.AccessList?.GetEffectiveLevel(mobile) ?? SecurityLevel.Denied;

                AddButton(10, y, 0xFA5, 0xFA7, EntryButtonBase + i, GumpButtonType.Reply, 0);
                AddLabelCropped(45, y + 2, 130, 18, 0x3E7, mobile.Name ?? "неизвестный");
                AddHtml(180, y, 130, 18, Colored(GetLevelName(level), GetHue(level)), false, false);

                y += RowHeight;
            }
        }
    }

    public static void DisplayTo(Mobile from, BaseGalleon galleon)
    {
        if (from?.NetState == null || galleon?.Deleted != false)
        {
            return;
        }

        if (!CanEdit(galleon, from))
        {
            from.SendMessage(0x22, "Только капитан может смотреть список доступа.");
            return;
        }

        from.CloseGump<ShipAccessListGump>();
        from.SendGump(new ShipAccessListGump(galleon));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _galleon?.Deleted != false || !CanEdit(_galleon, from))
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            ShipSecurityGump.DisplayTo(from, _galleon);
            return;
        }

        var index = info.ButtonID - EntryButtonBase;

        if (index < 0 || index >= _entries.Count)
        {
            return;
        }

        GrantAccessGump.DisplayTo(from, _galleon, _entries[index]);
    }

    private static List<Mobile> CollectEntries(BaseGalleon galleon)
    {
        var entries = new List<Mobile>();
        var list = galleon.AccessList;

        if (list == null)
        {
            return entries;
        }

        foreach (var (mobile, _) in list.Manifest)
        {
            if (mobile?.Deleted == false && !galleon.IsOwner(mobile))
            {
                entries.Add(mobile);
            }
        }

        return entries;
    }
}
