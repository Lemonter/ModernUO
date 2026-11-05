/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Gumps/NewMaginciaMessageGump.cs).
///
///     Почта Новой Маджинции: значок со счётчиком, список сообщений с постраничной навигацией и окно
///     подробностей с кнопкой удаления.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             ServUO строил это на <c>BaseGump</c> с <c>AddGumpLayout</c>/<c>Refresh</c>; здесь — обычные
///             <see cref="Gump" /> с конструктором на <see cref="Mobile" /> и статическим <c>DisplayTo</c>.
///         </item>
///         <item>Постраничный вывод упрощён до 10 строк на страницу с кнопками «назад/вперёд».</item>
///         <item>Собственные строки переведены на русский, cliloc-идентификаторы не тронуты.</item>
///     </list>
/// </summary>
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.NewMagincia;

public class NewMaginciaMessageGump : Gump
{
    private const int GreenColor = 0x4BB7;

    private readonly Mobile _user;
    private readonly List<NewMaginciaMessage> _messages;

    private NewMaginciaMessageGump(Mobile user, List<NewMaginciaMessage> messages) : base(490, 30)
    {
        _user = user;
        _messages = messages;

        AddPage(0);

        AddBackground(0, 0, 164, 32, 0x24B8);
        AddButton(7, 7, 0x1523, 0x1523, 1);
        AddHtmlLocalized(37, 7, 120, 18, 1150425, $"{messages.Count}", GreenColor, false, false); // ~1_COUNT~ Messages
    }

    /// <summary>Открывает значок, только если у игрока есть непросмотренные сообщения.</summary>
    public static void DisplayTo(Mobile from)
    {
        if (from == null)
        {
            return;
        }

        var messages = MaginciaLottoSystem.GetMessages(from);

        if (messages == null || messages.Count == 0)
        {
            return;
        }

        from.CloseGump<NewMaginciaMessageGump>();
        from.SendGump(new NewMaginciaMessageGump(from, messages));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            NewMaginciaMessageListGump.DisplayTo(_user);
        }
        else if (info.ButtonID == 0 && _messages.Count > 0)
        {
            DisplayTo(_user);
        }
    }
}

public class NewMaginciaMessageListGump : Gump
{
    private const int GreenColor = 0x4BB7;
    private const int LightBlueColor = 0x4AFD;
    private const int PageSize = 10;

    private readonly Mobile _user;
    private readonly List<NewMaginciaMessage> _messages;
    private readonly int _page;

    private NewMaginciaMessageListGump(Mobile user, List<NewMaginciaMessage> messages, int page) : base(490, 30)
    {
        _user = user;
        _messages = messages;
        _page = page;

        var pages = (messages.Count + PageSize - 1) / PageSize;

        AddBackground(0, 0, 314, 380, 0x24B8);
        AddButton(7, 7, 0x1523, 0x1523, 0);
        AddHtmlLocalized(47, 7, 194, 18, 1150425, $"{messages.Count}", GreenColor, false, false); // ~1_COUNT~ Messages

        var start = page * PageSize;
        var end = System.Math.Min(start + PageSize, messages.Count);

        for (var i = start; i < end; i++)
        {
            var message = messages[i];

            if (message == null)
            {
                continue;
            }

            var y = 34 + (i - start) * 32;

            if (message.Body?.Number > 0)
            {
                if (message.Args == null)
                {
                    AddHtmlLocalized(47, y, 260, 16, message.Body.Number, LightBlueColor, false, false);
                }
                else
                {
                    AddHtmlLocalized(47, y, 260, 16, message.Body.Number, message.Args, LightBlueColor, false, false);
                }
            }
            else
            {
                AddHtml(47, y, 260, 16, Colored(message.Body?.String ?? string.Empty, "#94BDEF"), false, false);
            }

            AddButton(7, y, 4029, 4031, i + 1000);
        }

        if (page > 0)
        {
            AddButton(7, 355, 0x1458, 0x1458, 2);
        }

        if (page + 1 < pages)
        {
            AddButton(290, 355, 0x1459, 0x1459, 3);
        }

        AddHtml(47, 355, 200, 18, Colored($"Страница {page + 1} из {System.Math.Max(1, pages)}", "#94BDEF"), false, false);
    }

    public static void DisplayTo(Mobile from, int page = 0)
    {
        if (from == null)
        {
            return;
        }

        var messages = MaginciaLottoSystem.GetMessages(from);

        if (messages == null || messages.Count == 0)
        {
            return;
        }

        var pages = (messages.Count + PageSize - 1) / PageSize;
        page = System.Math.Clamp(page, 0, System.Math.Max(0, pages - 1));

        from.CloseGump<NewMaginciaMessageListGump>();
        from.SendGump(new NewMaginciaMessageListGump(from, messages, page));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 0:
                {
                    NewMaginciaMessageGump.DisplayTo(_user);
                    break;
                }
            case 2:
                {
                    DisplayTo(_user, _page - 1);
                    break;
                }
            case 3:
                {
                    DisplayTo(_user, _page + 1);
                    break;
                }
            default:
                {
                    var index = info.ButtonID - 1000;

                    if (index >= 0 && index < _messages.Count)
                    {
                        NewMaginciaMessageDetailGump.DisplayTo(_user, _messages, index);
                    }

                    break;
                }
        }
    }

    private static string Colored(string text, string color) => $"<BASEFONT COLOR={color}>{text}</BASEFONT>";
}

public class NewMaginciaMessageDetailGump : Gump
{
    private const int GreenColor = 0x4BB7;
    private const int BlueColor = 0x110;

    private readonly Mobile _user;
    private readonly List<NewMaginciaMessage> _messages;
    private readonly NewMaginciaMessage _message;

    private NewMaginciaMessageDetailGump(Mobile user, List<NewMaginciaMessage> messages, NewMaginciaMessage message)
        : base(490, 30)
    {
        _user = user;
        _messages = messages;
        _message = message;

        AddPage(0);

        AddBackground(0, 0, 414, 341, 0x24B8);
        AddButton(7, 7, 0x1523, 0x1523, 0);
        AddButton(390, 7, 0x1519, 0x151A, 1);
        AddHtmlLocalized(47, 7, 360, 18, 1150425, $"{messages.Count}", GreenColor, false, false); // ~1_COUNT~ Messages

        if (message.Body?.Number > 0)
        {
            if (message.Args == null)
            {
                AddHtmlLocalized(7, 34, 404, 150, message.Body.Number, BlueColor, true, true);
            }
            else
            {
                AddHtmlLocalized(7, 34, 404, 150, message.Body.Number, message.Args, BlueColor, true, true);
            }
        }
        else
        {
            AddHtml(7, 34, 404, 150, $"<BASEFONT COLOR=#004284>{message.Body?.String}</BASEFONT>", true, true);
        }

        var left = message.Expires - Core.Now;

        if (left < System.TimeSpan.Zero)
        {
            left = System.TimeSpan.Zero;
        }

        // This message will expire in ~1_DAYS~ days, ~2_HOURS~ hours, and ~3_MIN~ minutes.
        AddHtmlLocalized(7, 194, 400, 18, 1150432, $"@{left.Days}@{left.Hours}@{left.Minutes}", GreenColor, false, false);

        AddHtmlLocalized(47, 212, 360, 22, 1150433, 0x76F2, false, false); // DELETE NOW
        AddButton(7, 212, 4005, 4007, 2);
    }

    public static void DisplayTo(Mobile from, List<NewMaginciaMessage> messages, int index)
    {
        if (from == null || messages == null || index < 0 || index >= messages.Count)
        {
            return;
        }

        from.CloseGump<NewMaginciaMessageDetailGump>();
        from.SendGump(new NewMaginciaMessageDetailGump(from, messages, messages[index]));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 0:
                {
                    NewMaginciaMessageGump.DisplayTo(_user);
                    break;
                }
            case 1:
                {
                    NewMaginciaMessageListGump.DisplayTo(_user);
                    break;
                }
            case 2:
                {
                    MaginciaLottoSystem.RemoveMessageFromQueue(_user, _message);

                    if (MaginciaLottoSystem.HasMessageInQueue(_user))
                    {
                        NewMaginciaMessageListGump.DisplayTo(_user);
                    }

                    break;
                }
        }
    }
}
