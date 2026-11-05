using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Все боевые выборы игрока — в одном месте: стойка, место удара, стиль, канал каста,
///     стихия, любимое заклинание.
///
///     ---- Зачем отдельно ----
///
///     Выбор теперь приходит двумя путями: кнопкой старого гампа и пакетом из Unity-окна. Если
///     проверки «хватает ли тактики» и сообщения игроку жили бы в каждом пути свои, они
///     разошлись бы при первой же правке — в одном окне стойку разрешили бы, в другом нет.
///     Поэтому оба пути зовут сюда, а сами только показывают.
///
///     После любого выбора — удачного или нет — Unity-окно получает свежее состояние: клиент
///     ничего не решает сам, и если сервер отказал, окно должно увидеть отказ, а не своё
///     предположение.
/// </summary>
public static class MahaonCombatChoices
{
    private const string FavoriteSpellTrack = "choice:favorite-spell";

    public static bool SetStance(PlayerMobile pm, CombatStance stance)
    {
        var ok = CombatStanceSystem.CanUseStance(pm, stance);

        if (ok)
        {
            CombatStanceSystem.SetStance(pm, stance);
            pm.SendMessage(0x59, $"Ты переходишь в {StanceAccusative(stance)} стойку.");
        }
        else
        {
            pm.SendMessage(0x22, "Твоей Тактики пока не хватает для этой стойки.");
        }

        MahaonCombatWindow.Push(pm);
        return ok;
    }

    public static bool SetLocation(PlayerMobile pm, HitLocation location)
    {
        var ok = HitLocationSystem.CanUseLocation(pm, location);

        if (ok)
        {
            HitLocationSystem.SetLocation(pm, location);
            pm.SendMessage(0x59, $"Ты целишься в {LocationAccusative(location)}.");
        }
        else
        {
            pm.SendMessage(0x22, "Твоей Тактики пока не хватает для такого удара.");
        }

        MahaonCombatWindow.Push(pm);
        return ok;
    }

    public static bool SetStyle(PlayerMobile pm, WeaponCategory category, byte style)
    {
        var ok = WeaponStyleSystem.SetActiveStyle(pm, category, style);

        if (ok)
        {
            pm.SendMessage(0x59, $"Стиль ({WeaponStyleSystem.RuCategoryName(category).ToLowerInvariant()}): {WeaponStyleSystem.StyleName(category, style)}.");
        }

        MahaonCombatWindow.Push(pm);
        return ok;
    }

    public static bool SetChannel(PlayerMobile pm, CastingChannel channel)
    {
        var ok = CastingChannelSystem.CanUseChannel(pm, channel);

        if (ok)
        {
            CastingChannelSystem.SetChannel(pm, channel);
            pm.SendMessage(0x59, $"Теперь ты колдуешь: {CastingChannelSystem.RuChannelName(channel).ToLowerInvariant()}.");
        }
        else
        {
            pm.SendMessage(0x22, "Твоей Магии пока не хватает для этого канала каста.");
        }

        MahaonCombatWindow.Push(pm);
        return ok;
    }

    public static bool SetArcheryStance(PlayerMobile pm, ArcheryStance stance)
    {
        var ok = ArcheryStanceSystem.Set(pm, stance);

        if (ok)
        {
            pm.SendMessage(0x59, $"Стойка лучника: {ArcheryStanceSystem.Name(stance).ToLowerInvariant()}.");
        }
        else
        {
            pm.SendMessage(0x22, $"Для навесной стрельбы нужна стрельба из лука выше {ArcheryStanceSystem.LoftedArcheryRequirement:0}.");
        }

        MahaonCombatWindow.Push(pm);
        return ok;
    }

    /// <summary>Выбрать стихию; null — снять.</summary>
    public static void SetElement(PlayerMobile pm, PreferredElement? element)
    {
        if (element == null)
        {
            PreferredElementSystem.ClearPreferred(pm);
            pm.SendMessage(0x59, "Предпочитаемая стихия снята.");
        }
        else
        {
            PreferredElementSystem.SetPreferred(pm, element.Value);
            pm.SendMessage(0x59, $"Предпочитаемая стихия: {PreferredElementSystem.RuElementName(element.Value)}.");
        }

        MahaonCombatWindow.Push(pm);
    }

    /// <summary>Как кнопка старого гампа: повторное нажатие на выбранную стихию снимает её.</summary>
    public static void ToggleElement(PlayerMobile pm, PreferredElement element) =>
        SetElement(pm, PreferredElementSystem.GetPreferred(pm) == element ? null : element);

    /// <summary>Любимое заклинание — для чека заклинания и шара. Ноль — не выбрано.</summary>
    public static void SetFavoriteSpell(PlayerMobile pm, int spellId)
    {
        MahaonProgress.Set(pm, FavoriteSpellTrack, spellId < 0 ? 0 : spellId);
        MahaonCombatWindow.Push(pm);
    }

    public static int GetFavoriteSpell(Mobile m) => (int)MahaonProgress.Get(m, FavoriteSpellTrack);

    // ---- Имена ------------------------------------------------------------------------------

    public static string StanceName(CombatStance stance) => stance switch
    {
        CombatStance.Normal       => "Обычная",
        CombatStance.Defensive    => "Защитная",
        CombatStance.Aggressive2x => "Агрессивная",
        CombatStance.Aggressive3x => "Яростная",
        _                         => stance.ToString()
    };

    private static string StanceAccusative(CombatStance stance) => stance switch
    {
        CombatStance.Normal       => "обычную",
        CombatStance.Defensive    => "защитную",
        CombatStance.Aggressive2x => "агрессивную",
        CombatStance.Aggressive3x => "яростную",
        _                         => stance.ToString()
    };

    public static string LocationName(HitLocation location) => location switch
    {
        HitLocation.Chest => "Грудь",
        HitLocation.Arms  => "Руки",
        HitLocation.Legs  => "Ноги",
        HitLocation.Hands => "Кисти",
        HitLocation.Back  => "Спина",
        HitLocation.Neck  => "Шея",
        HitLocation.Head  => "Голова",
        _                 => location.ToString()
    };

    private static string LocationAccusative(HitLocation location) => location switch
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
}
