/// <summary>
///     Аналог движкового <c>ConfirmDryDockGump</c> для галер (в ServUO отдельного окна не было — там
///     переиспользовался общий <c>ConfirmDryDockGump</c>).
///
///     Своё окно нужно по одной причине: <c>BaseBoat.EndDryDock</c> не виртуален и его вызов из
///     движкового окна обошёл бы <see cref="BaseGalleon.CompleteDryDock" />, где кормчий временно
///     убирается с палубы (иначе <c>CheckDryDock</c> считает его помехой). Текст — русский, кнопки
///     те же: «ПРОДОЛЖИТЬ» (id 2) и «ОТМЕНА» (id 1).
/// </summary>
using Server.Gumps;
using Server.Network;

namespace Server.Engines.HighSeas;

public class ShipDryDockGump : Gump
{
    private const int LabelColor = 0x7FFF;

    private readonly BaseGalleon _galleon;

    private ShipDryDockGump(BaseGalleon galleon) : base(150, 200)
    {
        _galleon = galleon;

        AddPage(0);

        AddBackground(0, 0, 220, 170, 5054);
        AddBackground(10, 10, 200, 150, 3000);

        AddHtml(20, 20, 180, 80, "<CENTER>Поставить этот корабль в сухой док?</CENTER>", false, false);

        AddButton(20, 100, 4005, 4007, 2);
        AddLabel(55, 100, LabelColor, "ПРОДОЛЖИТЬ");

        AddButton(20, 125, 4005, 4007, 1);
        AddLabel(55, 125, LabelColor, "ОТМЕНА");
    }

    public static void DisplayTo(Mobile from, BaseGalleon galleon)
    {
        if (from?.NetState == null || galleon?.Deleted != false)
        {
            return;
        }

        from.CloseGump<ShipDryDockGump>();
        from.SendGump(new ShipDryDockGump(galleon));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID != 2 || from == null || _galleon?.Deleted != false)
        {
            return;
        }

        _galleon.CompleteDryDock(from);
    }
}
