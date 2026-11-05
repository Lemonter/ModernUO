/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Gumps/BaseShipGump.cs).
///
///     Общая шапка окон корабля: фон, название галеры, владелец, палитра уровней доступа и их
///     названия. Как и остальные окна High Seas в этом репозитории — обычный <see cref="Gump" /> с
///     явным <c>DisplayTo</c> у наследников (см. <c>Engines/NewMagincia/Bazaar/Gumps</c>).
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Тексты ServUO шли cliloc-ами 1149724–1149786; здесь они заменены русскими строками
///             (<c>AddHtml</c>/<c>AddLabel</c>) — cliloc-и клиента остаются как есть в тех местах,
///             где они и были (названия предметов/сообщения), но подписи окна — авторские.
///         </item>
///         <item>
///             <c>GetLevel(SecurityLevel)</c> возвращал номер cliloc; теперь возвращает русское
///             название уровня.
///         </item>
///         <item>
///             Класс сделан абстрактным: отдельного «пустого» окна с одной шапкой не бывает.
///         </item>
///     </list>
/// </summary>
using Server.Gumps;

namespace Server.Engines.HighSeas;

public abstract class BaseShipGump : Gump
{
    public const int NaHue = 0x5EF7;
    public const int PassengerHue = 0x1CFF;
    public const int CrewHue = 0x1FE7;
    public const int OfficerHue = 0x7FE7;
    public const int DenyHue = 0x7CE7;
    public const int CaptainHue = 0x7DE7;
    public const int LabelColor = 0x7FFF;
    public const int NoHue = 0x3DEF;

    protected const int Width = 320;
    protected const int Height = 385;

    protected BaseShipGump(BaseGalleon galleon) : base(100, 100)
    {
        AddPage(0);
        AddBackground(0, 0, Width, Height, 0xA3C);

        AddHtml(10, 10, 300, 18, Centered(Colored("Пассажиры и команда", LabelColor)), false, false);

        AddLabel(10, 38, LabelColor, "Корабль:");
        AddLabel(80, 38, 0x53, string.IsNullOrEmpty(galleon.ShipName) ? "безымянный" : galleon.ShipName);

        AddLabel(10, 56, LabelColor, "Владелец:");
        AddLabel(80, 56, 0x53, galleon.Owner?.Name ?? "неизвестен");
    }

    protected static string Colored(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

    protected static string Centered(string text) => $"<CENTER>{text}</CENTER>";

    protected static int GetHue(SecurityLevel level) => level switch
    {
        SecurityLevel.Captain   => CaptainHue,
        SecurityLevel.Officer   => OfficerHue,
        SecurityLevel.Crewman   => CrewHue,
        SecurityLevel.Passenger => PassengerHue,
        SecurityLevel.Denied    => DenyHue,
        _                       => NaHue
    };

    protected static string GetLevelName(SecurityLevel level) => level switch
    {
        SecurityLevel.Captain   => "КАПИТАН",
        SecurityLevel.Officer   => "ОФИЦЕР",
        SecurityLevel.Crewman   => "МАТРОС",
        SecurityLevel.Passenger => "ПАССАЖИР",
        SecurityLevel.Denied    => "ОТКАЗАНО",
        _                       => "Н/Д"
    };

    protected static bool CanEdit(BaseGalleon galleon, Mobile from) =>
        galleon?.Deleted == false && galleon.GetSecurityLevel(from) == SecurityLevel.Captain;
}
