using System;
using System.Runtime.CompilerServices;
using Server.Mobiles;
using Server.Spells;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Этап 7: мастерство заклинаний — тот же слой, что мастерство оружия, только «вещь» —
///     заклинание (раздел 6 COMBAT-PROGRESSION.md).
///
///     ---- Как растёт ----
///
///     Только когда заклинание сработало по цели: нанесло урон, вылечило, наложило усиление
///     или ослабление, призвало существо. Каст в пустоту не считается. Одно заклинание — одна
///     попытка роста, сколько бы целей ни задела Цепная молния. Темп тот же, что у оружия:
///     шанс 15%, по 0.3 до 50, 0.15 до 80, 0.05 дальше; сообщение — на каждом целом уровне.
///
///     Пока учтены пути урона, лечения, усилений/ослаблений характеристик и призыва — через
///     общие точки SpellHelper, без правки каждого заклинания. Паралич, яд, поля и прочие
///     эффекты со своими путями растить мастерство пока не будут.
///
///     ---- Что даёт ----
///
///     Плавно до 100: мана −15%, время каста −10%, сила (урон и лечение) +10%.
///
///     ---- Линейка ----
///
///     Школа заклинания: мастерство Молнии даёт 25% соседям той же школы. Дорожка хранит
///     школу в ключе («mastery:spell/magery/Destruction/LightningSpell»), поэтому соседи
///     находятся по префиксу без перебора всех заклинаний.
/// </summary>
public static class SpellMasterySystem
{
    public const int MaxLevel = 100;
    public const int GainHue = 0x06B9;

    public const double MaxManaDiscount = 0.15;
    public const double MaxCastSpeedup = 0.10;
    public const double MaxPower = 0.10;

    private const double GainChance = 0.15;
    private const double LineSpill = 0.25;
    private const string Prefix = "mastery:spell/";

    /// <summary>Заклинания, которые уже попытались дать рост, — одна попытка на каст.</summary>
    private static readonly ConditionalWeakTable<Spell, object> Tried = new();

    private static readonly object Yes = new();

    public static string LineOf(Mobile m, ISpell spell) =>
        SpellSchools.TryDescribe(m, spell, out var info) ? info.Line : "other";

    public static string TrackOf(string line, Type spellType) => $"{Prefix}{line}/{spellType.Name}";

    public static string TrackOf(Mobile m, Spell spell) => TrackOf(LineOf(m, spell), spell.GetType());

    public static double Raw(Mobile m, Spell spell) => m == null || spell == null ? 0 : MahaonProgress.Get(m, TrackOf(m, spell));

    public static double Raw(Mobile m, string track) => m == null ? 0 : MahaonProgress.Get(m, track);

    /// <summary>Своё мастерство плюс четверть лучшего соседа по школе, не выше 100.</summary>
    public static double Effective(Mobile m, string track)
    {
        if (m == null)
        {
            return 0;
        }

        var own = MahaonProgress.Get(m, track);
        var linePrefix = track[..(track.LastIndexOf('/') + 1)];
        var best = 0.0;

        foreach (var (other, value) in MahaonProgress.TracksOf(m))
        {
            if (other != track && other.StartsWith(linePrefix, StringComparison.Ordinal) && value > best)
            {
                best = value;
            }
        }

        return Math.Min(MaxLevel, own + best * LineSpill);
    }

    public static double Effective(Mobile m, Spell spell) => spell == null ? 0 : Effective(m, TrackOf(m, spell));

    public static double ManaScalar(Mobile m, Spell spell) => 1.0 - MaxManaDiscount * Effective(m, spell) / MaxLevel;

    public static double CastDelayScalar(Mobile m, Spell spell) => 1.0 - MaxCastSpeedup * Effective(m, spell) / MaxLevel;

    public static double PowerScalar(Mobile m, Spell spell) => 1.0 + MaxPower * Effective(m, spell) / MaxLevel;

    /// <summary>Заклинание сработало по цели. Зовётся из общих точек SpellHelper.</summary>
    public static void OnEffect(Mobile caster, Spell spell)
    {
        if (caster is not PlayerMobile || spell == null || spell.Caster != caster)
        {
            return;
        }

        if (Tried.TryGetValue(spell, out _))
        {
            return;
        }

        Tried.AddOrUpdate(spell, Yes);

        if (!SpellSchools.TryDescribe(caster, spell, out _))
        {
            return; // заклинание вне школ (бусидо, ниндзюцу, мастерства) — мастерства нет
        }

        var track = TrackOf(caster, spell);
        var raw = MahaonProgress.Get(caster, track);

        if (raw >= MaxLevel || Utility.RandomDouble() > GainChance)
        {
            return;
        }

        Advance(caster, spell, track, raw, GainFor(raw));
    }

    /// <summary>Заклинание, которое сейчас в руках у мага, — для путей без явного заклинания.</summary>
    public static void OnEffect(Mobile caster) => OnEffect(caster, caster?.Spell as Spell);

    public static double GainFor(double raw) => raw < 50 ? 0.3 : raw < 80 ? 0.15 : 0.05;

    public static void Advance(Mobile m, Spell spell, string track, double raw, double gain)
    {
        var after = Math.Min(MaxLevel, raw + gain);
        MahaonProgress.Set(m, track, after);

        var was = (int)Math.Floor(raw);
        var now = (int)Math.Floor(after);

        if (now > was)
        {
            m.SendMessage(GainHue, $"Твоё владение заклинанием «{SpellSchools.RuName(spell)}» выросло на {now - was}. Теперь — {now}.");
        }

        MahaonSkillTree.NotifyChanged(m);
    }

    /// <summary>Строка доски: что даёт действующее мастерство.</summary>
    public static string BonusText(double effective) =>
        $"−{effective * MaxManaDiscount:0.#}% маны, −{effective * MaxCastSpeedup:0.#}% времени каста, +{effective * MaxPower:0.#}% силы";
}
