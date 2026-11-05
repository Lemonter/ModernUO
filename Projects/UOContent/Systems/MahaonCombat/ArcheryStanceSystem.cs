using System;
using Server.Items;

namespace Server.Systems.MahaonCombat;

public enum ArcheryStance : byte
{
    /// <summary>Прямой выстрел: как есть, без прибавок и без цены.</summary>
    Direct = 0,

    /// <summary>Навесной: лук поднят — бьёт вдвое дальше, но за пределом обычной дальности медленнее и неточнее.</summary>
    Lofted = 1,

    /// <summary>Прицельный: дольше целишься — точнее и чаще крит.</summary>
    Aimed = 2,

    /// <summary>Беглый: стреляешь быстрее, но ближе и неточнее.</summary>
    Rapid = 3
}

/// <summary>
///     Стойки лучника — выбор в моменте, и каждая чем-то платит.
///
///     ---- Откуда ----
///
///     У лучника уже было правило: при стрельбе из лука выше 90 можно бить вдвое дальше
///     дальности оружия, а за её пределом стрелять на 40% медленнее — «реальный лук бьёт
///     дальше, если поднять его, но чтобы попасть, тратишь больше времени». Работало оно само,
///     по порогу навыка. Теперь это стойка «Навесной» с тем же порогом и той же ценой, плюс
///     −10 к попаданию за пределом обычной дальности, а рядом встали прицельный и беглый
///     выстрелы.
///
///     ---- Кто ничего не выбирал ----
///
///     Ведёт себя как раньше: лук выше 90 — навесной, ниже — прямой. Обновление ничего не
///     отбирает молча; выбор появляется, а не навязывается.
///
///     ---- Где применяется ----
///
///     Дальность — Mobile.CheckCombatTime в ядре спрашивает у оружия GetCombatRange; ядро про
///     стойки не знает и знать не должно. Скорость — BaseWeapon.GetDelay. Попадание и крит —
///     вкладом в профиль удара, то есть видно в чеке.
/// </summary>
public static class ArcheryStanceSystem
{
    public const double LoftedArcheryRequirement = 90.0;

    private const string ChoiceTrack = "choice:archery-stance";

    public static bool CanUse(Mobile m, ArcheryStance stance) =>
        stance != ArcheryStance.Lofted || m.Skills[SkillName.Archery].Value > LoftedArcheryRequirement;

    /// <summary>Действующая стойка: выбранная, если её можно держать, иначе — как было до стоек.</summary>
    public static ArcheryStance Active(Mobile m)
    {
        if (m == null)
        {
            return ArcheryStance.Direct;
        }

        if (MahaonProgress.TryGet(m, ChoiceTrack, out var chosen) && Enum.IsDefined(typeof(ArcheryStance), (byte)chosen))
        {
            var stance = (ArcheryStance)(byte)chosen;
            return CanUse(m, stance) ? stance : ArcheryStance.Direct;
        }

        return CanUse(m, ArcheryStance.Lofted) ? ArcheryStance.Lofted : ArcheryStance.Direct;
    }

    /// <summary>Выбранная стойка как она записана — для окна, где нужно показать выбор.</summary>
    public static ArcheryStance Chosen(Mobile m) => Active(m);

    public static bool Set(Mobile m, ArcheryStance stance)
    {
        if (m == null || !CanUse(m, stance))
        {
            return false;
        }

        MahaonProgress.Set(m, ChoiceTrack, (byte)stance);
        return true;
    }

    /// <summary>На какой дистанции можно стрелять. Для не-стрелкового оружия — просто его дальность.</summary>
    public static int CombatRange(Mobile m, BaseWeapon weapon)
    {
        if (weapon == null)
        {
            return 1;
        }

        if (!weapon.IsRangedWeapon)
        {
            return weapon.MaxRange;
        }

        return Active(m) switch
        {
            ArcheryStance.Lofted => weapon.MaxRange * 2,
            ArcheryStance.Rapid  => Math.Max(1, weapon.MaxRange - 2),
            _                    => weapon.MaxRange
        };
    }

    /// <summary>Стреляет ли он сейчас за пределом обычной дальности — то есть навесом.</summary>
    public static bool BeyondNormalRange(Mobile m, BaseWeapon weapon, Mobile target) =>
        target != null && weapon is { IsRangedWeapon: true } && !m.InRange(target, weapon.MaxRange);

    /// <summary>Во сколько раз дольше выстрел: больше единицы — медленнее.</summary>
    public static double DelayFactor(Mobile m, BaseWeapon weapon)
    {
        if (weapon is not { IsRangedWeapon: true })
        {
            return 1.0;
        }

        return Active(m) switch
        {
            ArcheryStance.Lofted when BeyondNormalRange(m, weapon, m.Combatant) => 1.0 / 0.6,
            ArcheryStance.Aimed => 1.0 / 0.75,
            ArcheryStance.Rapid => 1.0 / 1.25,
            _                   => 1.0
        };
    }

    public static string Name(ArcheryStance stance) => stance switch
    {
        ArcheryStance.Lofted => "Навесной",
        ArcheryStance.Aimed  => "Прицельный",
        ArcheryStance.Rapid  => "Беглый",
        _                    => "Прямой выстрел"
    };

    public static string Trade(ArcheryStance stance) => stance switch
    {
        ArcheryStance.Lofted => "дальность ×2; за пределом обычной дальности −40% скорости и −10 к попаданию (лук 90+)",
        ArcheryStance.Aimed  => "−25% скорости, +15 к попаданию, +5% крит",
        ArcheryStance.Rapid  => "−2 к дальности, +25% скорости, −10 к попаданию",
        _                    => "без изменений"
    };

    // ---- Вклад в выстрел ------------------------------------------------------------------

    private sealed class Contributor : IStrikeContributor
    {
        public void Contribute(in StrikeContext context, StrikeProfile profile)
        {
            if (context.Weapon is not { IsRangedWeapon: true })
            {
                return;
            }

            var stance = Active(context.Attacker);
            var source = $"Стойка: {Name(stance).ToLowerInvariant()}";

            switch (stance)
            {
                case ArcheryStance.Lofted:
                {
                    profile.Add(source, StrikeField.Range, context.Weapon.MaxRange);

                    if (BeyondNormalRange(context.Attacker, context.Weapon, context.Defender))
                    {
                        profile.Add(source, StrikeField.HitChance, -10);
                    }

                    break;
                }

                case ArcheryStance.Aimed:
                {
                    profile.Add(source, StrikeField.HitChance, 15);
                    profile.Add(source, StrikeField.CritChance, 5);
                    break;
                }

                case ArcheryStance.Rapid:
                {
                    profile.Add(source, StrikeField.Range, -2);
                    profile.Add(source, StrikeField.HitChance, -10);
                    break;
                }
            }
        }
    }

    private static readonly Contributor Instance = new();

    public static void Configure() => Register();

    /// <summary>Повторный вызов безвреден — для тестов, где Configure не звали.</summary>
    public static void Register() => StrikeProfiles.Register(Instance);
}
