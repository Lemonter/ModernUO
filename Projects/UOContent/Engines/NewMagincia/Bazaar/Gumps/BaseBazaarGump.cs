/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Gumps/BaseBazaarGump.cs).
///
///     Общая основа окон базара: фон, кнопка закрытия и палитра/форматирование, которыми пользуются все
///     остальные окна подсистемы.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>BaseGump</c> → обычный <see cref="Gump" /> с явным <c>DisplayTo</c> у каждого окна (стиль
///             <c>Engines/NewMagincia/HousingLotto/Gumps</c>).
///         </item>
///         <item>
///             <c>String.Format</c> → интерполяция; <c>Color()</c>/<c>AlignRight()</c> остались строками
///             HTML-разметки, потому что их результат уходит в <c>AddHtml</c>, а не в handler-API.
///         </item>
///         <item>
///             Проверка <c>this is CommodityTargetGump</c> заменена флагом конструктора: окно выбора цели не
///             должно показывать кнопку закрытия, иначе клиент закроет его вместе с курсором прицела.
///         </item>
///     </list>
/// </summary>
using Server.Gumps;

namespace Server.Engines.NewMagincia;

public class BaseBazaarGump : Gump
{
    public const int RedColor = 0xB22222;
    public const int BlueColor = 0x000080;
    public const int OrangeColor = 0x804000;
    public const int GreenColor = 0x008040;
    public const int DarkGreenColor = 0x008000;
    public const int YellowColor = 0xFFFF00;
    public const int GrayColor = 0x808080;

    public const int RedColor16 = 0x4000;
    public const int BlueColor16 = 0x10;
    public const int OrangeColor16 = 0x4100;
    public const int GreenColor16 = 0x208;
    public const int DarkGreenColor16 = 0x200;
    public const int YellowColor16 = 0xFFE0;
    public const int GrayColor16 = 0xC618;

    public const int LabelHueBlue = 0xCC;

    protected BaseBazaarGump(int width, int height, bool showClose = true) : base(100, 100)
    {
        AddBackground(0, 0, width, height, 9300);

        if (!showClose)
        {
            return;
        }

        AddButton(width - 40, height - 30, 4020, 4022, 0, GumpButtonType.Reply, 0);
        AddHtmlLocalized(width - 150, height - 30, 100, 20, 1114514, "#1060675", 0x0, false, false); // CLOSE
    }

    protected static string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

    protected static string FormatAmt(int amount) => amount == 0 ? "0" : amount.ToString("###,###,###");

    protected static string FormatStallName(string text) => $"<DIV ALIGN=CENTER><i>{text}</i></DIV>";

    protected static string FormatBrokerName(string text) => $"<DIV ALIGN=CENTER>{text}</DIV>";

    protected static string AlignRight(string text) => $"<DIV ALIGN=RIGHT>{text}</DIV>";

    protected static string AlignLeft(string text) => $"<DIV ALIGN=LEFT>{text}</DIV>";
}
