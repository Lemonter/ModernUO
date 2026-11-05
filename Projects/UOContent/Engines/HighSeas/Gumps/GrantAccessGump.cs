/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Gumps/GrantAccessGump.cs).
///
///     Карточка одного персонажа в списке доступа: его действующий уровень (с учётом «публичного»
///     доступа, группы и гильдии) и кнопки «отказать / пассажир / матрос / офицер / капитан», а также
///     удаление из списка.
///
///     Замены: подписи — русскими строками вместо cliloc-ов 1149725–1149777; «Да/Нет» тоже текстом;
///     в <see cref="OnResponse" /> добавлена перепроверка прав капитана и запрет менять доступ
///     владельцу корабля (в ServUO такой проверки не было).
/// </summary>
using Server.Gumps;
using Server.Network;

namespace Server.Engines.HighSeas;

public class GrantAccessGump : BaseShipGump
{
    private readonly BaseGalleon _galleon;
    private readonly Mobile _player;

    private GrantAccessGump(Mobile player, BaseGalleon galleon) : base(galleon)
    {
        _player = player;
        _galleon = galleon;

        var list = galleon.AccessList;
        var level = list?.GetEffectiveLevel(player) ?? SecurityLevel.Denied;
        var inGuild = list?.IsInGuild(player) == true;
        var inParty = list?.IsInParty(player) == true;
        var isPublic = list?.IsPublic == true;
        var isListed = list?.Manifest.ContainsKey(player) == true;

        AddLabel(10, 79, LabelColor, "Игрок:");
        AddLabel(140, 79, 0x30, player.Name ?? "неизвестный");

        AddLabel(10, 97, LabelColor, "Действующий уровень:");
        AddHtml(140, 97, 160, 18, Colored(GetLevelName(level), GetHue(level)), false, false);

        AddLabel(10, 120, LabelColor, "Публичный доступ:");
        AddHtml(140, 120, 50, 18, Colored(isPublic ? "ДА" : "НЕТ", isPublic ? CrewHue : NoHue), false, false);

        if (isPublic && list != null)
        {
            AddHtml(
                200,
                120,
                100,
                18,
                Colored(GetLevelName(list.DefaultPublicAccess), GetHue(list.DefaultPublicAccess)),
                false,
                false
            );
        }

        AddLabel(10, 138, LabelColor, "В моей группе:");
        AddHtml(140, 138, 50, 18, Colored(inParty ? "ДА" : "НЕТ", inParty ? CrewHue : NoHue), false, false);

        if (inParty && list != null)
        {
            AddHtml(
                200,
                138,
                100,
                18,
                Colored(GetLevelName(list.DefaultPartyAccess), GetHue(list.DefaultPartyAccess)),
                false,
                false
            );
        }

        AddLabel(10, 156, LabelColor, "В моей гильдии:");
        AddHtml(140, 156, 50, 18, Colored(inGuild ? "ДА" : "НЕТ", inGuild ? CrewHue : NoHue), false, false);

        if (inGuild && list != null)
        {
            AddHtml(
                200,
                156,
                100,
                18,
                Colored(GetLevelName(list.DefaultGuildAccess), GetHue(list.DefaultGuildAccess)),
                false,
                false
            );
        }

        AddLabel(10, 179, LabelColor, "Статус списка доступа:");

        if (isListed)
        {
            AddButton(65, 197, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
            AddLabel(100, 199, NoHue, "УБРАТЬ ИЗ СПИСКА");
        }
        else
        {
            AddImage(65, 197, 0xFA6);
            AddLabel(100, 199, NoHue, "НЕ В СПИСКЕ");
        }

        AddLevelButton(215, 2, SecurityLevel.Denied, level);
        AddLevelButton(233, 3, SecurityLevel.Passenger, level);
        AddLevelButton(251, 4, SecurityLevel.Crewman, level);
        AddLevelButton(269, 5, SecurityLevel.Officer, level);
        AddLevelButton(287, 6, SecurityLevel.Captain, level);

        AddButton(10, 355, 0xFA5, 0xFA7, 7, GumpButtonType.Reply, 0);
        AddLabel(45, 357, LabelColor, "ГЛАВНОЕ МЕНЮ");

        AddButton(160, 355, 0xFA5, 0xFA7, 8, GumpButtonType.Reply, 0);
        AddLabel(195, 357, LabelColor, "СПИСОК ДОСТУПА");
    }

    public static void DisplayTo(Mobile from, BaseGalleon galleon, Mobile player)
    {
        if (from?.NetState == null || galleon?.Deleted != false || player?.Deleted != false)
        {
            return;
        }

        if (!CanEdit(galleon, from))
        {
            from.SendMessage(0x22, "Только капитан может менять доступ на корабль.");
            return;
        }

        from.CloseGump<GrantAccessGump>();
        from.SendGump(new GrantAccessGump(player, galleon));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || info.ButtonID == 0 || _galleon?.Deleted != false || !CanEdit(_galleon, from))
        {
            return;
        }

        if (_player?.Deleted != false)
        {
            ShipAccessListGump.DisplayTo(from, _galleon);
            return;
        }

        var list = _galleon.AccessList;

        if (list == null)
        {
            return;
        }

        if (_galleon.IsOwner(_player))
        {
            from.SendMessage(
                0x22,
                "Владелец корабля всегда капитан — его доступ изменить нельзя."
            );
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                list.RemoveFromAccessList(_player);
                ShipAccessListGump.DisplayTo(from, _galleon);
                return;
            case 2:
                list.AddToManifest(_player, SecurityLevel.Denied);
                break;
            case 3:
                list.AddToManifest(_player, SecurityLevel.Passenger);
                break;
            case 4:
                list.AddToManifest(_player, SecurityLevel.Crewman);
                break;
            case 5:
                list.AddToManifest(_player, SecurityLevel.Officer);
                break;
            case 6:
                list.AddToManifest(_player, SecurityLevel.Captain);
                break;
            case 7:
                ShipSecurityGump.DisplayTo(from, _galleon);
                return;
            case 8:
                ShipAccessListGump.DisplayTo(from, _galleon);
                return;
            default:
                return;
        }

        DisplayTo(from, _galleon, _player);
    }

    private void AddLevelButton(int y, int buttonId, SecurityLevel level, SecurityLevel current)
    {
        if (current == level)
        {
            AddImage(65, y, 0xFA6);
        }
        else
        {
            AddButton(65, y, 0xFA5, 0xFA7, buttonId, GumpButtonType.Reply, 0);
        }

        AddLabel(100, y + 2, current == level ? GetHue(level) : LabelColor, GetLevelName(level));
    }
}
