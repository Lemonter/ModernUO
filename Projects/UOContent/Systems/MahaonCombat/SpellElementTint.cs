using Server.Spells;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Эффекты заклинания урона — в цвет выбранной стихии: маг природы бросает зелёный
///     огненный шар, маг холода — ледяную молнию.
///
///     ---- Как ----
///
///     Ядро (Effects) перед отправкой любого эффекта спрашивает ActiveHueOverride. Мы отвечаем
///     оттенком, пока идёт применение заклинания: от успешного CheckSequence (мана списана,
///     заклинание действительно летит) до FinishSequence. Для надёжности оттенок живёт только в
///     пределах того же такта сервера — эффект из таймера через секунду уже не перекрасится, и
///     зависшего «окраса» не бывает, даже если заклинание не дошло до FinishSequence.
///
///     Красим только заклинания урона (школы огня/разрушения Магии и школы урона остальных) —
///     у них стихия и меняет урон. Огненное заклинание у мага огня не трогаем: оно и так огонь.
///
///     ---- Для клиента ----
///
///     Цвет уходит обычным полем hue в пакетах эффектов, поэтому работает и в старом клиенте.
///     Unity может узнавать стихию по этим четырём оттенкам и рисовать свои частицы (таблица —
///     в COMBAT-UI-TASK.md).
/// </summary>
public static class SpellElementTint
{
    public const int FireHue = 0x0489;   // hues.mul «fire»
    public const int NatureHue = 0x0044; // зелёный
    public const int ColdHue = 0x0482;   // hues.mul «ice_hue_3»
    public const int EnergyHue = 0x007D; // фиолетовый

    private static int _hue;
    private static long _tick;

    public static void Configure() => Effects.ActiveHueOverride = Current;

    private static int Current() => _hue != 0 && Core.TickCount == _tick ? _hue : 0;

    /// <summary>Заклинание прошло проверку и сейчас применяется.</summary>
    public static void Begin(Spell spell)
    {
        _hue = spell == null ? 0 : HueFor(spell.Caster, spell);
        _tick = Core.TickCount;
    }

    public static void End() => _hue = 0;

    public static int HueOf(PreferredElement element) => element switch
    {
        PreferredElement.Fire   => FireHue,
        PreferredElement.Poison => NatureHue,
        PreferredElement.Cold   => ColdHue,
        _                       => EnergyHue
    };

    public static int HueFor(Mobile caster, ISpell spell)
    {
        if (caster == null || PreferredElementSystem.GetPreferred(caster) is not { } element)
        {
            return 0;
        }

        if (!SpellSchools.TryDescribe(caster, spell, out var info) || !IsDamageLine(info.Line))
        {
            return 0;
        }

        if (element == PreferredElement.Fire && info.Line == "magery/Fire")
        {
            return 0;
        }

        return HueOf(element);
    }

    private static bool IsDamageLine(string line) =>
        line is "magery/Fire" or "magery/Destruction" || line.EndsWith("/Damage", System.StringComparison.Ordinal);
}
