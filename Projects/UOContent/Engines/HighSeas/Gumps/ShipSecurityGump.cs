/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Gumps/ShipSecurityGump.cs).
///
///     Настройки доступа галеры: как на борт пускает членство в группе, а также уровни доступа
///     «для всех», «для группы» и «для гильдии». Кнопка «СПИСОК ДОСТУПА» ведёт в
///     <see cref="ShipAccessListGump" />.
///
///     Замены ServUO-овских cliloc-ов 1149725–1149786 на русские строки; добавлена проверка прав
///     в <see cref="OnResponse" /> — ответ гумпа может прислать кто угодно, поэтому уровень капитана
///     перепроверяется на каждое нажатие (в исходнике такой проверки не было).
/// </summary>
using Server.Gumps;
using Server.Network;

namespace Server.Engines.HighSeas;

public class ShipSecurityGump : BaseShipGump
{
    /// <summary>Порядок уровней в панели выбора: id кнопки = база + (int)уровня.</summary>
    private static readonly SecurityLevel[] LevelOptions =
    {
        SecurityLevel.NA,
        SecurityLevel.Denied,
        SecurityLevel.Passenger,
        SecurityLevel.Crewman,
        SecurityLevel.Officer
    };

    private readonly BaseGalleon _galleon;
    private readonly Mobile _from;

    private ShipSecurityGump(Mobile from, BaseGalleon galleon) : base(galleon)
    {
        _from = from;
        _galleon = galleon;

        var list = galleon.AccessList;
        var partyAccess = list.PartyAccess;

        AddLabel(10, 79, LabelColor, "Доступ по группе:");

        AddChoice(55, 97, partyAccess == PartyAccess.Never, 1001);
        AddLabel(90, 99, LabelColor, "Никогда");

        AddChoice(55, 115, partyAccess == PartyAccess.LeaderOnly, 1002);
        AddLabel(90, 117, LabelColor, "Когда я лидер группы");

        AddChoice(55, 133, partyAccess == PartyAccess.MemberOnly, 1003);
        AddLabel(90, 135, LabelColor, "Когда я в группе");

        AddLabel(10, 158, LabelColor, "Публичный доступ:");
        AddButton(140, 156, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 2);
        AddLabel(175, 158, GetHue(list.DefaultPublicAccess), GetLevelName(list.DefaultPublicAccess));

        AddLabel(10, 175, LabelColor, "Доступ группе:");
        AddButton(140, 173, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 3);
        AddLabel(175, 175, GetHue(list.DefaultPartyAccess), GetLevelName(list.DefaultPartyAccess));

        AddLabel(10, 193, LabelColor, "Доступ гильдии:");
        AddButton(140, 191, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 4);
        AddLabel(175, 193, GetHue(list.DefaultGuildAccess), GetLevelName(list.DefaultGuildAccess));

        AddButton(160, 355, 0xFA5, 0xFA7, 2000, GumpButtonType.Reply, 0);
        AddLabel(195, 357, LabelColor, "СПИСОК ДОСТУПА");

        AddPage(2);
        AddLevelPanel(30, list.DefaultPublicAccess, 1100);

        AddPage(3);
        AddLevelPanel(70, list.DefaultPartyAccess, 1200);

        AddPage(4);
        AddLevelPanel(110, list.DefaultGuildAccess, 1300);
    }

    public static void DisplayTo(Mobile from, BaseGalleon galleon)
    {
        if (from?.NetState == null || galleon?.Deleted != false)
        {
            return;
        }

        if (!CanEdit(galleon, from))
        {
            from.SendMessage(0x22, "Только капитан может менять доступ на корабль.");
            return;
        }

        from.CloseGump<ShipSecurityGump>();
        from.SendGump(new ShipSecurityGump(from, galleon));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || info.ButtonID == 0 || !CanEdit(_galleon, from))
        {
            return;
        }

        var list = _galleon.AccessList;

        if (list == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1001:
                list.PartyAccess = PartyAccess.Never;
                break;
            case 1002:
                list.PartyAccess = PartyAccess.LeaderOnly;
                break;
            case 1003:
                list.PartyAccess = PartyAccess.MemberOnly;
                break;
            case 2000:
                ShipAccessListGump.DisplayTo(from, _galleon);
                return;
            default:
                if (!TrySetLevel(list, info.ButtonID))
                {
                    return;
                }

                break;
        }

        DisplayTo(from, _galleon);
    }

    private static bool TrySetLevel(ShipAccessList list, int buttonId)
    {
        if (buttonId is >= 1100 and <= 1104)
        {
            list.DefaultPublicAccess = (SecurityLevel)(buttonId - 1100);
            return true;
        }

        if (buttonId is >= 1200 and <= 1204)
        {
            list.DefaultPartyAccess = (SecurityLevel)(buttonId - 1200);
            return true;
        }

        if (buttonId is >= 1300 and <= 1304)
        {
            list.DefaultGuildAccess = (SecurityLevel)(buttonId - 1300);
            return true;
        }

        return false;
    }

    private void AddChoice(int x, int y, bool selected, int buttonId)
    {
        if (selected)
        {
            AddImage(x, y, 0xFA6);
            return;
        }

        AddButton(x, y, 0xFA5, 0xFA7, buttonId, GumpButtonType.Reply, 0);
    }

    private void AddLevelPanel(int offsetX, SecurityLevel current, int buttonBase)
    {
        AddBackground(offsetX, 215, 190, 130, 0xA3C);

        var y = 243;

        foreach (var level in LevelOptions)
        {
            AddChoice(offsetX + 10, y, current == level, buttonBase + (int)level);

            AddLabel(offsetX + 45, y + 2, current == level ? GetHue(level) : LabelColor, GetLevelName(level));

            y += 18;
        }
    }
}
