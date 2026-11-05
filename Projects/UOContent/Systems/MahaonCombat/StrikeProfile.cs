using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Commands;
using Server.Items;
using Server.Targeting;

namespace Server.Systems.MahaonCombat;

/// <summary>Во что именно вклад: из этих корзин складывается удар.</summary>
public enum StrikeField : byte
{
    /// <summary>Прибавка к шансу попасть, в процентных пунктах.</summary>
    HitChance,

    /// <summary>Шанс крита, в процентах.</summary>
    CritChance,

    /// <summary>Прибавка к урону, в процентах. Та самая, что раньше копилась в percentageBonus.</summary>
    Damage,

    /// <summary>Прибавка к скорости удара, в процентах.</summary>
    SwingSpeed,

    /// <summary>Прибавка к дальности, в тайлах.</summary>
    Range
}

/// <summary>Один вклад: кто, во что и сколько. Из этих строк и получается чек.</summary>
public readonly record struct StrikeLine(string Source, StrikeField Field, double Value);

/// <summary>То, о чём спрашивают вкладчика: кто бьёт, кого и чем.</summary>
public readonly record struct StrikeContext(Mobile Attacker, Mobile Defender, BaseWeapon Weapon);

/// <summary>
///     Система, которая хочет что-то добавить к удару, регистрируется здесь один раз и дальше
///     спрашивается на каждом ударе.
///
///     Вкладчик обязан быть ЧИСТЫМ: только смотреть и добавлять строки. Его зовут и при броске
///     на попадание, и при подсчёте урона, а со временем — и для чека в боевом окне, где удара
///     нет вовсе. Всё, что расходуется в момент удара (выбранное место, приём), сюда не годится
///     и добавляется прямо в BaseWeapon.OnHit.
/// </summary>
public interface IStrikeContributor
{
    void Contribute(in StrikeContext context, StrikeProfile profile);
}

/// <summary>
///     Профиль одного удара: все вклады с подписями и их суммы по корзинам.
///
///     ---- Зачем ----
///
///     Раньше урон оружия собирался в одну целую переменную двадцатью восемью строками
///     «percentageBonus += …» — ванильные слееры, наши стойки, стили, металлы, профессии
///     вперемешку. Итог был виден только в виде урона, а откуда он взялся — нигде. Шанс
///     попадания и крит наши системы не трогали вовсе, потому что трогать было нечем.
///
///     Теперь каждое слагаемое — строка с подписью. Сумма та же, но её можно показать: это и
///     есть чек в боевом окне, и это же — способ проверить, что баланс сложился так, как
///     задуман, а не как получилось.
/// </summary>
public sealed class StrikeProfile
{
    /// <summary>Потолки корзин. Урон режется отдельно, ванильным правилом (×3), — см. BaseWeapon.</summary>
    public const double MaxHitChance = 25.0;

    public const double MaxCritChance = 30.0;

    /// <summary>Во сколько раз крит увеличивает урон.</summary>
    public const double CritMultiplier = 1.5;

    private readonly List<StrikeLine> _lines = new();

    public IReadOnlyList<StrikeLine> Lines => _lines;

    /// <summary>Выпал ли крит. Отдельным флагом, а не строкой урона: крит умножает уже
    /// посчитанный урон, и строка «+50%» в той же корзине соврала бы о том, как он сложился.</summary>
    public bool Critical { get; set; }

    /// <summary>Нулевые вклады не записываем: в чеке им нечего делать.</summary>
    public void Add(string source, StrikeField field, double value)
    {
        if (value != 0)
        {
            _lines.Add(new StrikeLine(source, field, value));
        }
    }

    /// <summary>Сумма корзины как есть, без потолков.</summary>
    public double Sum(StrikeField field)
    {
        var total = 0.0;

        foreach (var line in _lines)
        {
            if (line.Field == field)
            {
                total += line.Value;
            }
        }

        return total;
    }

    /// <summary>Прибавка к попаданию с учётом потолка, в процентных пунктах.</summary>
    public double HitChance => Math.Clamp(Sum(StrikeField.HitChance), -MaxHitChance, MaxHitChance);

    /// <summary>Шанс крита с учётом потолка, в процентах.</summary>
    public double CritChance => Math.Clamp(Sum(StrikeField.CritChance), 0.0, MaxCritChance);
}

/// <summary>Реестр вкладчиков и память о последнем ударе.</summary>
public static class StrikeProfiles
{
    private static readonly List<IStrikeContributor> Contributors = new();

    /// <summary>Последний удар каждого бойца — для [StrikeInfo и, позже, для чека в боевом окне.
    /// Слабая таблица: удалённый мобиль уносит свою запись с собой.</summary>
    private static readonly ConditionalWeakTable<Mobile, StrikeProfile> Last = new();

    public static void Register(IStrikeContributor contributor)
    {
        if (contributor != null && !Contributors.Contains(contributor))
        {
            Contributors.Add(contributor);
        }
    }

    public static void Unregister(IStrikeContributor contributor) => Contributors.Remove(contributor);

    /// <summary>
    ///     Прибавка к попаданию для броска в CheckHit, в процентных пунктах, уже с потолком.
    ///
    ///     Отдельно от Gather, потому что спрашивается на КАЖДОМ взмахе, включая промахи, и
    ///     пока вкладчиков нет, заводить ради нуля профиль со списком незачем.
    /// </summary>
    public static double HitChanceBonus(Mobile attacker, Mobile defender, BaseWeapon weapon) =>
        Contributors.Count == 0 ? 0.0 : Gather(attacker, defender, weapon).HitChance;

    /// <summary>Свежий профиль со вкладами всех зарегистрированных систем.</summary>
    public static StrikeProfile Gather(Mobile attacker, Mobile defender, BaseWeapon weapon)
    {
        var profile = new StrikeProfile();

        if (Contributors.Count == 0 || attacker == null || weapon == null)
        {
            return profile;
        }

        var context = new StrikeContext(attacker, defender, weapon);

        foreach (var contributor in Contributors)
        {
            contributor.Contribute(context, profile);
        }

        return profile;
    }

    public static void Remember(Mobile attacker, StrikeProfile profile)
    {
        if (attacker != null && profile != null)
        {
            Last.AddOrUpdate(attacker, profile);
        }
    }

    public static StrikeProfile LastOf(Mobile attacker) =>
        attacker != null && Last.TryGetValue(attacker, out var profile) ? profile : null;

    // ---- Команда ----------------------------------------------------------------------------

    public static void Configure()
    {
        CommandSystem.Register("StrikeInfo", AccessLevel.GameMaster, StrikeInfo_OnCommand);
    }

    [Usage("StrikeInfo")]
    [Description("Из чего сложился последний удар бойца: каждый вклад с подписью.")]
    private static void StrikeInfo_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x3B2, "Чей последний удар показать?");
        e.Mobile.Target = new InfoTarget();
    }

    private static string FieldName(StrikeField field) => field switch
    {
        StrikeField.HitChance  => "попадание",
        StrikeField.CritChance => "крит",
        StrikeField.Damage     => "урон",
        StrikeField.SwingSpeed => "скорость",
        StrikeField.Range      => "дальность",
        _                      => field.ToString()
    };

    private class InfoTarget : Target
    {
        public InfoTarget() : base(16, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile who)
            {
                from.SendMessage(0x22, "Это не боец.");
                return;
            }

            var profile = LastOf(who);

            if (profile == null)
            {
                from.SendMessage(0x3B2, $"{who.Name} ещё не наносил ударов с последнего перезапуска.");
                return;
            }

            from.SendMessage(0x59, $"Последний удар {who.Name}:");

            if (profile.Lines.Count == 0)
            {
                from.SendMessage(0x3B2, "  ни одного вклада — голый удар");
                return;
            }

            foreach (var line in profile.Lines)
            {
                var sign = line.Value > 0 ? "+" : "";
                from.SendMessage(0x3B2, $"  {FieldName(line.Field)}: {sign}{line.Value:0.#}  — {line.Source}");
            }

            from.SendMessage(
                0x59,
                $"Итог: урон {profile.Sum(StrikeField.Damage):+0;-0;0}% (потолок +300), " +
                $"попадание {profile.HitChance:+0.#;-0.#;0}, крит {profile.CritChance:0.#}%" +
                (profile.Critical ? " — КРИТ" : "")
            );
        }
    }
}
