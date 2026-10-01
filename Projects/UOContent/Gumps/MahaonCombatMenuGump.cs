using Server.Network;
using Server.Systems.MahaonCombat;

namespace Server.Gumps;

/// <summary>
///     Always with the player once logged in (see PlayerMobile.OnLogin) — no real close;
///     the old "Закрыть" button now just collapses it down to a small strip instead of
///     dismissing it, and pressing ESC/the client's own X (which the underlying gump
///     protocol reports as button 0, same as before) does the same rather than actually
///     closing — BaseGump (the newer gump system everything else here uses) doesn't expose
///     a native "Closable = false" the way the older legacy Gump class does, so this is
///     done by just always resending instead of ever letting nothing come back.
///
///     Three tabs now: "Бой" (stances + hit locations — the original layout), "Магия"
///     (casting channel + preferred element), and "Стрельба" (Archery>90 extended-range
///     shooting status — see HitLocationSystem's hit-location buttons, which already work
///     the same for ranged weapons too, so this tab is informational rather than another set
///     of choice buttons). All three live on the same gump instance/state so switching tabs
///     doesn't lose the collapsed/expanded state.
/// </summary>
public class MahaonCombatMenuGump : DynamicGump
{
    private const int CombatTab = 0;
    private const int MagicTab = 1;
    private const int ArcheryTab = 2;

    private readonly Mobile _player;
    private readonly bool _collapsed;
    private readonly int _tab;

    public override bool Singleton => true;

    public MahaonCombatMenuGump(Mobile player, bool collapsed = false, int tab = CombatTab) : base(50, 50)
    {
        _player = player;
        _collapsed = collapsed;
        _tab = tab;
    }

    // Раскладка кнопок: 0 = закрытие клиентом (ESC/крестик) — трактуется как сворачивание.
    // 1 = развернуть/свернуть, 2/3/4 = переключение вкладок, 10-13 = стойки,
    // 20-26 = места удара, 30-32 = канал каста, 40-43 = предпочитаемая стихия.
    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (_collapsed)
        {
            BuildCollapsed(ref builder);
            return;
        }

        builder.AddPage();
        builder.AddBackground(0, 0, 320, 410, 5054);
        builder.AddAlphaRegion(10, 10, 300, 390);

        builder.AddButton(280, 15, 2437, 2436, 1); // collapse, top-right corner

        AddTabButton(ref builder, 15, 15, 2, "Бой", _tab == CombatTab);
        AddTabButton(ref builder, 110, 15, 3, "Магия", _tab == MagicTab);
        AddTabButton(ref builder, 205, 15, 4, "Стрельба", _tab == ArcheryTab);

        switch (_tab)
        {
            case MagicTab:
                BuildMagicTab(ref builder);
                break;
            case ArcheryTab:
                BuildArcheryTab(ref builder);
                break;
            default:
                BuildCombatTab(ref builder);
                break;
        }

        builder.AddButton(15, 380, 4014, 4016, 1);
        builder.AddHtml(50, 380, 150, 20, "Свернуть");
    }

    private void BuildCombatTab(ref DynamicGumpBuilder builder)
    {
        var tactics = _player.Skills[SkillName.Tactics].Value;
        var stance = CombatStanceSystem.GetStance(_player);

        builder.AddHtml(15, 40, 290, 20, $"Тактика {tactics:F1}");
        builder.AddHtml(15, 60, 290, 20, "Стойка:");
        AddStanceButton(ref builder, 15, 85, 10, CombatStance.Normal, "Обычная", stance, true);
        AddStanceButton(ref builder, 15, 110, 11, CombatStance.Defensive, "Защитная (вдвое меньше урона наносишь/получаешь)", stance, true);
        AddStanceButton(ref builder, 15, 135, 12, CombatStance.Aggressive2x, "Агрессивная x2 (нужна Тактика 60)", stance,
            CombatStanceSystem.CanUseStance(_player, CombatStance.Aggressive2x));
        AddStanceButton(ref builder, 15, 160, 13, CombatStance.Aggressive3x, "Агрессивная x3 (нужна Тактика 100)", stance,
            CombatStanceSystem.CanUseStance(_player, CombatStance.Aggressive3x));

        builder.AddHtml(15, 195, 290, 20, "Целиться в: (от этого зависит и стиль боя)");
        AddLocationButton(ref builder, 15, 220, 20, HitLocation.Chest, "Грудь", true);
        AddLocationButton(ref builder, 15, 245, 21, HitLocation.Arms, "Руки", true);
        AddLocationButton(ref builder, 15, 270, 22, HitLocation.Legs, "Ноги", true);
        AddLocationButton(ref builder, 15, 295, 23, HitLocation.Hands, "Кисти", true);
        AddLocationButton(ref builder, 15, 320, 26, HitLocation.Head, "Голова", true);
        AddLocationButton(ref builder, 15, 345, 25, HitLocation.Back, "Спина (нужна Тактика 70)",
            HitLocationSystem.CanUseLocation(_player, HitLocation.Back));
        AddLocationButton(ref builder, 160, 220, 24, HitLocation.Neck, "Шея x2 (нужна Тактика 115)",
            HitLocationSystem.CanUseLocation(_player, HitLocation.Neck));
    }

    private void BuildMagicTab(ref DynamicGumpBuilder builder)
    {
        var magery = _player.Skills[SkillName.Magery].Value;
        var channel = CastingChannelSystem.GetChannel(_player);
        var preferred = PreferredElementSystem.GetPreferred(_player);

        builder.AddHtml(15, 40, 290, 20, $"Магия {magery:F1}");
        builder.AddHtml(15, 60, 290, 20, "Каст:");
        AddChannelButton(ref builder, 15, 85, 30, CastingChannel.Hands, "Руками", channel, true);
        AddChannelButton(ref builder, 15, 110, 31, CastingChannel.Voice, "Голосом (нужна Магия 50)", channel,
            CastingChannelSystem.CanUseChannel(_player, CastingChannel.Voice));
        AddChannelButton(ref builder, 15, 135, 32, CastingChannel.Mind, "Мыслью (нужна Магия 100)", channel,
            CastingChannelSystem.CanUseChannel(_player, CastingChannel.Mind));

        builder.AddHtml(15, 170, 290, 34, "Если по выбранному каналу попадут (руки/шея/голова) — 5 сек нельзя колдовать.");

        builder.AddHtml(15, 215, 290, 20, "Предпочитаемая стихия (половина урона заклинаний уходит в неё):");
        AddElementButton(ref builder, 15, 240, 40, PreferredElement.Fire, "Огонь", preferred);
        AddElementButton(ref builder, 15, 265, 41, PreferredElement.Poison, "Природа", preferred);
        AddElementButton(ref builder, 15, 290, 42, PreferredElement.Energy, "Энергия", preferred);
        AddElementButton(ref builder, 15, 315, 43, PreferredElement.Cold, "Холод", preferred);
    }

    private void BuildArcheryTab(ref DynamicGumpBuilder builder)
    {
        var archery = _player.Skills[SkillName.Archery].Value;
        var weapon = _player.Weapon as IWeapon;
        var extended = archery > 90.0;

        builder.AddHtml(15, 40, 290, 20, $"Стрельба из лука {archery:F1}");

        builder.AddHtml(
            15, 65, 290, 54,
            extended
                ? "Больше 90 — можешь стрелять и за пределами дальности оружия, но на 40% медленнее. Как только цель окажется в пределах обычной дальности оружия — скорость снова станет нормальной."
                : "Пока не больше 90 — стрельба ограничена обычной дальностью оружия. При 90+ появится возможность стрелять дальше ценой скорости."
        );

        if (weapon is { IsRangedWeapon: true })
        {
            var inNormalRange = _player.Combatant == null || _player.InRange(_player.Combatant, weapon.MaxRange);
            builder.AddHtml(15, 125, 290, 20, $"Дальность оружия: {weapon.MaxRange}");

            if (extended && _player.Combatant != null)
            {
                builder.AddHtml(
                    15, 150, 290, 20,
                    inNormalRange ? "Цель в пределах дальности — скорость обычная." : "Цель дальше обычной дальности — стрельба на 40% медленнее."
                );
            }
        }
        else
        {
            builder.AddHtml(15, 125, 290, 20, "В руках не лук/арбалет.");
        }
    }

    private void BuildCollapsed(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 130, 40, 5054);
        builder.AddAlphaRegion(5, 5, 120, 30);
        builder.AddButton(5, 8, 4005, 4007, 1); // expand
        builder.AddHtml(35, 10, 90, 20, "Бой");
    }

    private void AddTabButton(ref DynamicGumpBuilder builder, int x, int y, int buttonId, string label, bool active)
    {
        builder.AddButton(x, y, active ? 4006 : 4005, active ? 4006 : 4007, buttonId);
        builder.AddLabel(x + 25, y, active ? 0x59 : 0x480, label);
    }

    private void AddStanceButton(
        ref DynamicGumpBuilder builder, int x, int y, int buttonId, CombatStance stance, string label,
        CombatStance current, bool enabled
    )
    {
        if (enabled)
        {
            builder.AddButton(x, y, 4005, 4007, buttonId);
        }
        else
        {
            builder.AddImage(x, y, 4020);
        }

        var hue = current == stance ? 0x59 : 0x480;
        builder.AddLabel(x + 25, y, hue, label);
    }

    private void AddLocationButton(
        ref DynamicGumpBuilder builder, int x, int y, int buttonId, HitLocation location, string label, bool enabled
    )
    {
        if (enabled)
        {
            builder.AddButton(x, y, 4005, 4007, buttonId);
        }
        else
        {
            builder.AddImage(x, y, 4020);
        }

        var hue = HitLocationSystem.GetChosenLocation(_player) == location ? 0x59 : 0x480;
        builder.AddLabel(x + 25, y, hue, label);
    }

    private void AddChannelButton(
        ref DynamicGumpBuilder builder, int x, int y, int buttonId, CastingChannel channel, string label,
        CastingChannel current, bool enabled
    )
    {
        if (enabled)
        {
            builder.AddButton(x, y, 4005, 4007, buttonId);
        }
        else
        {
            builder.AddImage(x, y, 4020);
        }

        var hue = current == channel ? 0x59 : 0x480;
        builder.AddLabel(x + 25, y, hue, label);
    }

    private void AddElementButton(
        ref DynamicGumpBuilder builder, int x, int y, int buttonId, PreferredElement element, string label,
        PreferredElement? current
    )
    {
        builder.AddButton(x, y, 4005, 4007, buttonId);
        var hue = current == element ? 0x59 : 0x480;
        builder.AddLabel(x + 25, y, hue, label);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        // 0 covers both an actual click on our own collapse/expand-adjacent close area AND
        // the client's own ESC/X (the underlying gump protocol reports those as button 0
        // too) — either way, just toggle collapsed state instead of letting it vanish.
        if (info.ButtonID == 0 || info.ButtonID == 1)
        {
            _player.SendGump(new MahaonCombatMenuGump(_player, !_collapsed, _tab));
            return;
        }

        switch (info.ButtonID)
        {
            case 2: _player.SendGump(new MahaonCombatMenuGump(_player, false, CombatTab)); return;
            case 3: _player.SendGump(new MahaonCombatMenuGump(_player, false, MagicTab)); return;
            case 4: _player.SendGump(new MahaonCombatMenuGump(_player, false, ArcheryTab)); return;

            case 10: SetStance(CombatStance.Normal); return;
            case 11: SetStance(CombatStance.Defensive); return;
            case 12: SetStance(CombatStance.Aggressive2x); return;
            case 13: SetStance(CombatStance.Aggressive3x); return;

            case 20: SetLocation(HitLocation.Chest); return;
            case 21: SetLocation(HitLocation.Arms); return;
            case 22: SetLocation(HitLocation.Legs); return;
            case 23: SetLocation(HitLocation.Hands); return;
            case 25: SetLocation(HitLocation.Back); return;
            case 24: SetLocation(HitLocation.Neck); return;
            case 26: SetLocation(HitLocation.Head); return;

            case 30: SetChannel(CastingChannel.Hands); return;
            case 31: SetChannel(CastingChannel.Voice); return;
            case 32: SetChannel(CastingChannel.Mind); return;

            case 40: SetElement(PreferredElement.Fire); return;
            case 41: SetElement(PreferredElement.Poison); return;
            case 42: SetElement(PreferredElement.Energy); return;
            case 43: SetElement(PreferredElement.Cold); return;
        }
    }

    private static string StanceRu(CombatStance stance) => stance switch
    {
        CombatStance.Normal       => "обычную",
        CombatStance.Defensive    => "защитную",
        CombatStance.Aggressive2x => "агрессивную x2",
        CombatStance.Aggressive3x => "агрессивную x3",
        _                         => stance.ToString()
    };

    private static string LocationRu(HitLocation location) => location switch
    {
        HitLocation.Chest => "грудь",
        HitLocation.Arms  => "руки",
        HitLocation.Legs  => "ноги",
        HitLocation.Hands => "кисти",
        HitLocation.Back  => "спину",
        HitLocation.Neck  => "шею",
        HitLocation.Head  => "голову",
        _                 => location.ToString()
    };

    private void SetStance(CombatStance stance)
    {
        if (!CombatStanceSystem.CanUseStance(_player, stance))
        {
            _player.SendMessage(0x22, "Твоей Тактики пока не хватает для этой стойки.");
            _player.SendGump(new MahaonCombatMenuGump(_player, false, _tab));
            return;
        }

        CombatStanceSystem.SetStance(_player, stance);
        _player.SendMessage(0x59, $"Ты переходишь в {StanceRu(stance)} стойку.");
        _player.SendGump(new MahaonCombatMenuGump(_player, false, _tab));
    }

    private void SetLocation(HitLocation location)
    {
        if (!HitLocationSystem.CanUseLocation(_player, location))
        {
            _player.SendMessage(0x22, "Твоей Тактики пока не хватает для такого удара.");
            _player.SendGump(new MahaonCombatMenuGump(_player, false, _tab));
            return;
        }

        HitLocationSystem.SetLocation(_player, location);
        _player.SendMessage(0x59, $"Ты целишься в {LocationRu(location)}.");
        Systems.MahaonCombat.MahaonSkillTree.NotifyChanged(_player); // derived style may have changed
        _player.SendGump(new MahaonCombatMenuGump(_player, false, _tab));
    }

    private void SetChannel(CastingChannel channel)
    {
        if (!CastingChannelSystem.CanUseChannel(_player, channel))
        {
            _player.SendMessage(0x22, "Твоей Магии пока не хватает для этого канала каста.");
            _player.SendGump(new MahaonCombatMenuGump(_player, false, MagicTab));
            return;
        }

        CastingChannelSystem.SetChannel(_player, channel);
        _player.SendMessage(0x59, $"Теперь ты колдуешь: {CastingChannelSystem.RuChannelName(channel).ToLowerInvariant()}.");
        _player.SendGump(new MahaonCombatMenuGump(_player, false, MagicTab));
    }

    private void SetElement(PreferredElement element)
    {
        if (PreferredElementSystem.GetPreferred(_player) == element)
        {
            PreferredElementSystem.ClearPreferred(_player);
            _player.SendMessage(0x59, "Предпочитаемая стихия снята.");
        }
        else
        {
            PreferredElementSystem.SetPreferred(_player, element);
            _player.SendMessage(0x59, $"Предпочитаемая стихия: {PreferredElementSystem.RuElementName(element)}.");
        }

        _player.SendGump(new MahaonCombatMenuGump(_player, false, MagicTab));
    }
}
