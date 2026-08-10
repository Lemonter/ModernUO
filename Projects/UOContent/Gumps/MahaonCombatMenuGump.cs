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
/// </summary>
public class MahaonCombatMenuGump : DynamicGump
{
    private readonly Mobile _player;
    private readonly bool _collapsed;

    public override bool Singleton => true;

    public MahaonCombatMenuGump(Mobile player, bool collapsed = false) : base(50, 50)
    {
        _player = player;
        _collapsed = collapsed;
    }

    // Раскладка кнопок: 0 = закрытие клиентом (ESC/крестик) — трактуется как сворачивание.
    // 1 = развернуть/свернуть, 10-13 = стойки, 20-24 = места удара.
    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (_collapsed)
        {
            BuildCollapsed(ref builder);
            return;
        }

        var tactics = _player.Skills[SkillName.Tactics].Value;
        var stance = CombatStanceSystem.GetStance(_player);

        builder.AddPage();
        builder.AddBackground(0, 0, 320, 360, 5054);
        builder.AddAlphaRegion(10, 10, 300, 340);

        builder.AddHtml(15, 15, 250, 20, $"Боевое меню (Тактика {tactics:F1})");
        builder.AddButton(280, 15, 2437, 2436, 1); // collapse, top-right corner
        builder.AddHtml(15, 40, 290, 20, "Стойка:");
        AddStanceButton(ref builder, 15, 65, 10, CombatStance.Normal, "Обычная", stance, true);
        AddStanceButton(ref builder, 15, 90, 11, CombatStance.Defensive, "Защитная (вдвое меньше урона наносишь/получаешь)", stance, true);
        AddStanceButton(ref builder, 15, 115, 12, CombatStance.Aggressive2x, "Агрессивная x2 (нужна Тактика 60)", stance,
            CombatStanceSystem.CanUseStance(_player, CombatStance.Aggressive2x));
        AddStanceButton(ref builder, 15, 140, 13, CombatStance.Aggressive3x, "Агрессивная x3 (нужна Тактика 100)", stance,
            CombatStanceSystem.CanUseStance(_player, CombatStance.Aggressive3x));

        builder.AddHtml(15, 175, 290, 20, "Целиться в:");
        AddLocationButton(ref builder, 15, 200, 20, HitLocation.Chest, "Грудь", true);
        AddLocationButton(ref builder, 15, 225, 21, HitLocation.Arms, "Руки", true);
        AddLocationButton(ref builder, 15, 250, 22, HitLocation.Legs, "Ноги", true);
        AddLocationButton(ref builder, 15, 275, 23, HitLocation.Hands, "Кисти", true);
        AddLocationButton(ref builder, 15, 300, 24, HitLocation.Neck, "Шея x2 (нужна Тактика 115)",
            HitLocationSystem.CanUseLocation(_player, HitLocation.Neck));

        builder.AddButton(15, 330, 4014, 4016, 1);
        builder.AddHtml(50, 330, 150, 20, "Свернуть");
    }

    private void BuildCollapsed(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 130, 40, 5054);
        builder.AddAlphaRegion(5, 5, 120, 30);
        builder.AddButton(5, 8, 4005, 4007, 1); // expand
        builder.AddHtml(35, 10, 90, 20, "Бой");
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

        builder.AddLabel(x + 25, y, 0x480, label);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        // 0 covers both an actual click on our own collapse/expand-adjacent close area AND
        // the client's own ESC/X (the underlying gump protocol reports those as button 0
        // too) — either way, just toggle collapsed state instead of letting it vanish.
        if (info.ButtonID == 0 || info.ButtonID == 1)
        {
            _player.SendGump(new MahaonCombatMenuGump(_player, !_collapsed));
            return;
        }

        switch (info.ButtonID)
        {
            case 10: SetStance(CombatStance.Normal); break;
            case 11: SetStance(CombatStance.Defensive); break;
            case 12: SetStance(CombatStance.Aggressive2x); break;
            case 13: SetStance(CombatStance.Aggressive3x); break;

            case 20: SetLocation(HitLocation.Chest); break;
            case 21: SetLocation(HitLocation.Arms); break;
            case 22: SetLocation(HitLocation.Legs); break;
            case 23: SetLocation(HitLocation.Hands); break;
            case 24: SetLocation(HitLocation.Neck); break;
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
        HitLocation.Neck  => "шею",
        _                 => location.ToString()
    };

    private void SetStance(CombatStance stance)
    {
        if (!CombatStanceSystem.CanUseStance(_player, stance))
        {
            _player.SendMessage(0x22, "Твоей Тактики пока не хватает для этой стойки.");
            _player.SendGump(new MahaonCombatMenuGump(_player));
            return;
        }

        CombatStanceSystem.SetStance(_player, stance);
        _player.SendMessage(0x59, $"Ты переходишь в {StanceRu(stance)} стойку.");
        _player.SendGump(new MahaonCombatMenuGump(_player));
    }

    private void SetLocation(HitLocation location)
    {
        if (!HitLocationSystem.CanUseLocation(_player, location))
        {
            _player.SendMessage(0x22, "Твоей Тактики пока не хватает для такого удара.");
            _player.SendGump(new MahaonCombatMenuGump(_player));
            return;
        }

        HitLocationSystem.SetPending(_player, location);
        _player.SendMessage(0x59, $"Ты целишься в {LocationRu(location)} для следующего удара.");
        _player.SendGump(new MahaonCombatMenuGump(_player));
    }
}
