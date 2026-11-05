using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Spells;

namespace Server.Systems.MahaonCombat;

/// <summary>Что делает перк школы.</summary>
public enum SchoolPerkKind : byte
{
    Mana,        // заклинания школы дешевле на N%
    Cast,        // каст быстрее на N%
    Power,       // урон и лечение сильнее на N%
    Duration,    // усиления, ослабления и призывы держатся дольше на N%
    Reagents,    // N% шанс не потратить реагенты
    SummonPower, // призванные крепче и больнее на N%
    Burn,        // огненные заклинания поджигают: N% урона ещё за три секунды
    SpellCrit,   // N% шанс, что заклинание ударит в полтора раза сильнее
    CurePoison,  // лечение заодно снимает яд
    Info         // описание того, что уже даёт своя система (без эффекта здесь)
}

public readonly record struct SchoolPerk(int Threshold, SchoolPerkKind Kind, int Value, string Text = null);

/// <summary>
///     Этап 8: перки школ магии на 25 / 50 / 75 / 100 (раздел 2.2 COMBAT-PROGRESSION.md).
///
///     ---- Что было ----
///
///     Школа давала плавную скидку на ману (до −20%) и, у школ урона, прибавку к урону (до
///     +18%). Эти плавные прибавки остаются — «каждое очко что-то значит». Сверху на 25/50/75/100
///     открываются ступени: конкретные эффекты своей темы.
///
///     ---- Правило ветки ----
///
///     Ни один перк не трогает шанс успеха заклинания — это дело навыка. Перки меняют цену,
///     время, силу, длительность и то, что происходит при успехе.
///
///     ---- Где работают ----
///
///     Только в общих точках, без правки отдельных заклинаний: Spell.ScaleMana, GetCastDelay,
///     ConsumeReagents; SpellHelper.Damage, Heal, AddStatBonus/AddStatCurse, Summon. Поэтому
///     «длительность» стоит только у школ, чьи заклинания идут через эти точки (усиления и
///     ослабления Магии, призывы Магии и Плетения), а у школ без реагентов (Рыцарство,
///     Плетение) нет перков на реагенты — пустых перков нет.
/// </summary>
public static class SchoolPerks
{
    private static SchoolPerk P(int t, SchoolPerkKind k, int v = 0, string text = null) => new(t, k, v, text);

    private static readonly Dictionary<string, SchoolPerk[]> Table = new()
    {
        // ---- Магия ----
        ["magery/Fire"] = [P(25, SchoolPerkKind.Power, 5), P(50, SchoolPerkKind.Reagents, 15), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.Burn, 25)],
        ["magery/Destruction"] = [P(25, SchoolPerkKind.Power, 5), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.SpellCrit, 10)],
        ["magery/Summoning"] = [P(25, SchoolPerkKind.Duration, 25), P(50, SchoolPerkKind.SummonPower, 15), P(75, SchoolPerkKind.Reagents, 20), P(100, SchoolPerkKind.SummonPower, 25)],
        ["magery/Holy"] = [P(25, SchoolPerkKind.Power, 10), P(50, SchoolPerkKind.Duration, 25), P(75, SchoolPerkKind.Mana, 10), P(100, SchoolPerkKind.CurePoison)],
        ["magery/Control"] = [P(25, SchoolPerkKind.Duration, 25), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.Duration, 25)],
        ["magery/Utility"] = [P(25, SchoolPerkKind.Reagents, 15), P(50, SchoolPerkKind.Mana, 15), P(75, SchoolPerkKind.Cast, 15), P(100, SchoolPerkKind.Reagents, 25)],

        // ---- Некромантия ----
        ["necromancy/Damage"] = [P(25, SchoolPerkKind.Power, 5), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.SpellCrit, 10)],
        ["necromancy/Curse"] = [P(25, SchoolPerkKind.Mana, 10), P(50, SchoolPerkKind.Reagents, 15), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.Mana, 15)],
        ["necromancy/Summoning"] =
        [
            P(25, SchoolPerkKind.Reagents, 10), P(50, SchoolPerkKind.Info, 0, "второй фамильяр"),
            P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.Info, 0, "третий фамильяр")
        ],
        ["necromancy/Transformation"] = [P(25, SchoolPerkKind.Mana, 15), P(50, SchoolPerkKind.Reagents, 15), P(75, SchoolPerkKind.Cast, 15), P(100, SchoolPerkKind.Mana, 15)],
        ["necromancy/Exorcism"] = [P(25, SchoolPerkKind.Power, 10), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.Power, 15)],

        // ---- Рыцарство (без реагентов) ----
        ["chivalry/Healing"] = [P(25, SchoolPerkKind.Power, 10), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.CurePoison)],
        ["chivalry/Combat"] = [P(25, SchoolPerkKind.Mana, 10), P(50, SchoolPerkKind.Cast, 10), P(75, SchoolPerkKind.Mana, 10), P(100, SchoolPerkKind.Cast, 15)],
        ["chivalry/Sacrifice"] = [P(25, SchoolPerkKind.Mana, 10), P(50, SchoolPerkKind.Cast, 10), P(75, SchoolPerkKind.Mana, 10), P(100, SchoolPerkKind.Cast, 15)],

        // ---- Мистицизм ----
        ["mysticism/Damage"] = [P(25, SchoolPerkKind.Power, 5), P(50, SchoolPerkKind.Reagents, 15), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.SpellCrit, 10)],
        ["mysticism/Utility"] = [P(25, SchoolPerkKind.Reagents, 15), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.Reagents, 25)],
        ["mysticism/Healing"] = [P(25, SchoolPerkKind.Power, 10), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.Power, 15)],

        // ---- Плетение чар (без реагентов) ----
        ["spellweaving/Damage"] = [P(25, SchoolPerkKind.Power, 5), P(50, SchoolPerkKind.Mana, 10), P(75, SchoolPerkKind.Cast, 10), P(100, SchoolPerkKind.SpellCrit, 10)],
        ["spellweaving/Healing"] = [P(25, SchoolPerkKind.Mana, 10), P(50, SchoolPerkKind.Cast, 10), P(75, SchoolPerkKind.Mana, 10), P(100, SchoolPerkKind.Cast, 15)],
        ["spellweaving/Summoning"] = [P(25, SchoolPerkKind.Duration, 25), P(50, SchoolPerkKind.SummonPower, 15), P(75, SchoolPerkKind.Mana, 10), P(100, SchoolPerkKind.SummonPower, 25)],
        ["spellweaving/Transformation"] = [P(25, SchoolPerkKind.Mana, 15), P(50, SchoolPerkKind.Cast, 10), P(75, SchoolPerkKind.Mana, 10), P(100, SchoolPerkKind.Cast, 15)],
        ["spellweaving/WeaponEnchant"] = [P(25, SchoolPerkKind.Mana, 10), P(50, SchoolPerkKind.Cast, 10), P(75, SchoolPerkKind.Mana, 10), P(100, SchoolPerkKind.Cast, 15)],
    };

    public static IReadOnlyList<SchoolPerk> PerksOf(string line) =>
        line != null && Table.TryGetValue(line, out var perks) ? perks : Array.Empty<SchoolPerk>();

    /// <summary>Сумма открытых перков этого вида у школы заклинания.</summary>
    public static int Sum(Mobile caster, ISpell spell, SchoolPerkKind kind)
    {
        if (caster == null || spell == null || !SpellSchools.TryDescribe(caster, spell, out var info))
        {
            return 0;
        }

        var total = 0;

        foreach (var perk in PerksOf(info.Line))
        {
            if (perk.Kind == kind && info.Value >= perk.Threshold)
            {
                total += Math.Max(perk.Value, 1);
            }
        }

        return total;
    }

    public static bool Has(Mobile caster, ISpell spell, SchoolPerkKind kind) => Sum(caster, spell, kind) > 0;

    // ---- Общие точки ---------------------------------------------------------------------

    public static double ManaScalar(Mobile caster, ISpell spell) => 1.0 - Sum(caster, spell, SchoolPerkKind.Mana) / 100.0;

    public static double CastScalar(Mobile caster, ISpell spell) => 1.0 - Sum(caster, spell, SchoolPerkKind.Cast) / 100.0;

    public static double PowerScalar(Mobile caster, ISpell spell) => 1.0 + Sum(caster, spell, SchoolPerkKind.Power) / 100.0;

    public static bool SavesReagents(Mobile caster, ISpell spell)
    {
        var chance = Sum(caster, spell, SchoolPerkKind.Reagents);
        return chance > 0 && Utility.Random(100) < chance;
    }

    /// <summary>Длительность усиления, ослабления или призыва — по заклинанию, которое сейчас у мага.</summary>
    public static TimeSpan ScaleDuration(Mobile caster, TimeSpan duration)
    {
        var bonus = Sum(caster, caster?.Spell as ISpell, SchoolPerkKind.Duration);
        return bonus > 0 ? TimeSpan.FromTicks((long)(duration.Ticks * (1.0 + bonus / 100.0))) : duration;
    }

    /// <summary>Призванное существо крепче и больнее — до призыва, по заклинанию у мага.</summary>
    public static void EmpowerSummon(Mobile caster, BaseCreature creature)
    {
        var bonus = Sum(caster, caster?.Spell as ISpell, SchoolPerkKind.SummonPower);

        if (bonus <= 0 || creature == null)
        {
            return;
        }

        var k = 1.0 + bonus / 100.0;

        if (creature.HitsMaxSeed > 0)
        {
            creature.HitsMaxSeed = (int)(creature.HitsMaxSeed * k);
        }

        creature.Hits = creature.HitsMax;
        creature.DamageMin = (int)Math.Round(creature.DamageMin * k);
        creature.DamageMax = (int)Math.Round(creature.DamageMax * k);
    }

    /// <summary>Перед уроном заклинания: магический крит. Возвращает урон.</summary>
    public static int RollSpellCrit(Mobile caster, ISpell spell, Mobile target, int damage)
    {
        var chance = Sum(caster, spell, SchoolPerkKind.SpellCrit);

        if (chance <= 0 || damage <= 0 || Utility.Random(100) >= chance)
        {
            return damage;
        }

        target?.PrivateOverheadMessage(MessageType.Regular, 0x22, false, "крит!", caster.NetState);
        return damage * 3 / 2;
    }

    /// <summary>После урона огненного заклинания — поджог: доля урона ещё за три секунды.</summary>
    public static void TryBurn(Mobile caster, ISpell spell, Mobile target, int damageGiven)
    {
        var share = Sum(caster, spell, SchoolPerkKind.Burn);

        if (share <= 0 || damageGiven <= 0 || target == null || !target.Alive)
        {
            return;
        }

        // Горение — настоящее состояние, как яд: тикает, гаснет в воде и под дождём, видно
        // клиенту (BurningSystem).
        BurningSystem.Ignite(target, caster, Math.Max(1, damageGiven * share / 100 / 3), 3);
    }

    /// <summary>Лечение заклинанием школы с этим перком снимает яд.</summary>
    public static void TryCurePoison(Mobile caster, ISpell spell, Mobile target)
    {
        if (target?.Poisoned == true && Has(caster, spell, SchoolPerkKind.CurePoison))
        {
            target.CurePoison(caster);
            target.SendMessage(0x59, "Лечение снимает яд.");
        }
    }

    // ---- Описания и объявления -----------------------------------------------------------

    public static string Describe(SchoolPerk perk) => perk.Kind switch
    {
        SchoolPerkKind.Mana        => $"заклинания школы дешевле на {perk.Value}%",
        SchoolPerkKind.Cast        => $"каст быстрее на {perk.Value}%",
        SchoolPerkKind.Power       => $"урон и лечение школы сильнее на {perk.Value}%",
        SchoolPerkKind.Duration    => $"усиления, ослабления и призывы держатся на {perk.Value}% дольше",
        SchoolPerkKind.Reagents    => $"{perk.Value}% шанс не потратить реагенты",
        SchoolPerkKind.SummonPower => $"призванные на {perk.Value}% крепче и больнее",
        SchoolPerkKind.Burn        => $"огонь поджигает: ещё {perk.Value}% урона за три секунды",
        SchoolPerkKind.SpellCrit   => $"{perk.Value}% шанс, что заклинание ударит в полтора раза сильнее",
        SchoolPerkKind.CurePoison  => "лечение заодно снимает яд",
        _                          => perk.Text ?? ""
    };

    /// <summary>Школа перешла ступень — сказать игроку, что открылось. Зовут школьные системы.</summary>
    public static void AnnounceUnlock(Mobile m, string line, double before, double after)
    {
        if (m is not PlayerMobile)
        {
            return;
        }

        foreach (var perk in PerksOf(line))
        {
            if (before < perk.Threshold && after >= perk.Threshold)
            {
                m.SendMessage(WeaponMasterySystem.GainHue, $"Открыт перк школы ({perk.Threshold}): {Describe(perk)}.");
            }
        }
    }

    /// <summary>Устойчивый ключ школы по навыку и русскому имени ветки — для доски.</summary>
    public static string LineOf(int skillId, string branchName)
    {
        switch ((SkillName)skillId)
        {
            case SkillName.Magery:
                foreach (var s in Enum.GetValues<MagerySchool>())
                {
                    if (MagerySchoolSystem.RuSchoolName(s) == branchName) return $"magery/{s}";
                }

                break;
            case SkillName.Necromancy:
                foreach (var s in Enum.GetValues<NecromancySchool>())
                {
                    if (NecromancySchoolSystem.RuSchoolName(s) == branchName) return $"necromancy/{s}";
                }

                break;
            case SkillName.Chivalry:
                foreach (var s in Enum.GetValues<ChivalrySchool>())
                {
                    if (ChivalrySchoolSystem.RuSchoolName(s) == branchName) return $"chivalry/{s}";
                }

                break;
            case SkillName.Mysticism:
                foreach (var s in Enum.GetValues<MysticismSchool>())
                {
                    if (MysticismSchoolSystem.RuSchoolName(s) == branchName) return $"mysticism/{s}";
                }

                break;
            case SkillName.Spellweaving:
                foreach (var s in Enum.GetValues<SpellweavingSchool>())
                {
                    if (SpellweavingSchoolSystem.RuSchoolName(s) == branchName) return $"spellweaving/{s}";
                }

                break;
        }

        return null;
    }
}
