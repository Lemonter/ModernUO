using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Слабые места существ — чтобы бой с драконом не был боем с мешком песка.
///
///     ---- У кого какие ----
///
///     По семействам, а семейства берутся из групп слееров: они уже знают, кто дракон, кто
///     нежить, кто гуманоид, — заводить вторую классификацию значило бы, что однажды они
///     разойдутся. Порядок проверки важен: дракон — тоже рептилия, и драконья строка должна
///     сработать раньше рептильей.
///
///     У игроков и людей слабое место не фиксировано — это слот, где броня хуже всего держит
///     твой тип урона. Его ищет HitLocationSystem.GetBestLocation (до сих пор им пользовались
///     только боты), а бонус за него уже даёт сам прицел. Здесь — только подсказка.
///
///     ---- Как узнать ----
///
///     В бою, не чаще раза в 10 секунд на цель, после попадания идёт проверка навыка:
///     анатомия — для гуманоидов и нежити, знание животных — для зверей, драконов, рептилий и
///     пауков, лучший из двух — для демонов. Сложность — от славы существа. Проверка настоящая
///     (CheckSkill), то есть и навык на ней растёт.
///
///     ---- Как использовать ----
///
///     Слабое место бьёт сильнее, только если оно ОТКРЫТО этим игроком и тактики хватает
///     целиться туда. Крепкое место — драконья спина — держит удар всегда, знаешь ты о нём
///     или нет: знание тут нужно, чтобы туда не бить.
///
///     ---- Знание копится ----
///
///     Открытое запоминается навсегда за видом существа (решено 05.11.2026): однажды
///     разобрался с драконами — в следующем бою знаешь сразу.
/// </summary>
public static class MahaonWeakPoints
{
    public readonly record struct Point(HitLocation Location, double Multiplier);

    public sealed record Family(string Key, string Name, bool ByAnatomy, bool ByAnimalLore, Point[] Points);

    // Стартовые числа — крутить при обкатке.
    private static readonly (SlayerName Slayer, Family Family)[] BySlayer =
    {
        (SlayerName.DragonSlaying, new Family("dragons", "драконы", false, true,
            new[] { new Point(HitLocation.Neck, 1.8), new Point(HitLocation.Back, 0.7) })),
        (SlayerName.ArachnidDoom, new Family("arachnids", "паукообразные", false, true,
            new[] { new Point(HitLocation.Legs, 1.6) })),
        (SlayerName.ReptilianDeath, new Family("reptiles", "рептилии", false, true,
            new[] { new Point(HitLocation.Head, 1.5) })),
        (SlayerName.Silver, new Family("undead", "нежить", true, false,
            new[] { new Point(HitLocation.Head, 1.6), new Point(HitLocation.Chest, 0.6) })),
        (SlayerName.Repond, new Family("humanoids", "гуманоиды", true, false,
            new[] { new Point(HitLocation.Neck, 1.4), new Point(HitLocation.Head, 1.3) })),
        (SlayerName.Exorcism, new Family("demons", "демоны", true, true,
            new[] { new Point(HitLocation.Head, 1.3) }))
    };

    private static readonly Family Animals = new("animals", "звери", false, true,
        new[] { new Point(HitLocation.Legs, 1.3), new Point(HitLocation.Neck, 1.3) });

    public const int MessageHue = 0x0035;

    private static readonly TimeSpan RollCooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Семейство существа. null — либо слабых мест нет вовсе (элементали бьются целиком),
    ///     либо это игрок или человек: у них слабость — худший слот брони (см. IsArmored).
    /// </summary>
    public static Family FamilyOf(Mobile m)
    {
        if (m == null || IsArmored(m))
        {
            return null;
        }

        foreach (var (slayer, family) in BySlayer)
        {
            if (SlayerGroup.GetEntryByName(slayer)?.Slays(m) == true)
            {
                return family;
            }
        }

        return m.Body.IsAnimal ? Animals : null;
    }

    /// <summary>Игрок или человек: слабость у него — в броне, а не в теле.</summary>
    public static bool IsArmored(Mobile m) => m is PlayerMobile || m.Body.IsHuman;

    // ---- Знание -------------------------------------------------------------------------------

    private static string KnowledgeTrack(Mobile target) => $"known:weak/{target.GetType().Name}";

    private static int Bit(HitLocation location) => 1 << (int)location;

    public static int KnownMask(Mobile player, Mobile target) =>
        target == null ? 0 : (int)MahaonProgress.Get(player, KnowledgeTrack(target));

    public static bool IsKnown(Mobile player, Mobile target, HitLocation location) =>
        (KnownMask(player, target) & Bit(location)) != 0;

    /// <summary>Что игрок уже знает об этом существе — для окна и гампа.</summary>
    public static List<Point> Known(Mobile player, Mobile target)
    {
        var known = new List<Point>();
        var family = FamilyOf(target);

        if (family == null)
        {
            return known;
        }

        var mask = KnownMask(player, target);

        foreach (var point in family.Points)
        {
            if ((mask & Bit(point.Location)) != 0)
            {
                known.Add(point);
            }
        }

        return known;
    }

    public static void Learn(Mobile player, Mobile target, HitLocation location) =>
        MahaonProgress.Set(player, KnowledgeTrack(target), KnownMask(player, target) | Bit(location));

    // ---- Открытие в бою -----------------------------------------------------------------------

    private sealed class Cooldown
    {
        public Mobile Target;
        public DateTime Next;
    }

    private static readonly ConditionalWeakTable<Mobile, Cooldown> Cooldowns = new();

    /// <summary>Сложность разглядеть слабость: от славы существа, 0–110.</summary>
    public static double Difficulty(Mobile target) =>
        target is BaseCreature bc ? Math.Clamp(bc.Fame / 200.0, 0.0, 110.0) : 50.0;

    /// <summary>После попадания — шанс разглядеть слабость. Зовётся из BaseWeapon.OnHit.</summary>
    public static void TryDiscover(Mobile attacker, Mobile defender)
    {
        if (attacker is not PlayerMobile pm || defender == null || !defender.Alive || defender == attacker)
        {
            return;
        }

        var cooldown = Cooldowns.GetOrCreateValue(pm);

        if (cooldown.Target == defender && Core.Now < cooldown.Next)
        {
            return;
        }

        cooldown.Target = defender;
        cooldown.Next = Core.Now + RollCooldown;

        if (IsArmored(defender))
        {
            ReadArmor(pm, defender);
            return;
        }

        var family = FamilyOf(defender);

        if (family == null)
        {
            return;
        }

        var mask = KnownMask(pm, defender);
        var unknown = new List<Point>();

        foreach (var point in family.Points)
        {
            if ((mask & Bit(point.Location)) == 0)
            {
                unknown.Add(point);
            }
        }

        if (unknown.Count == 0)
        {
            return; // всё уже известно — и проверять нечего
        }

        var difficulty = Difficulty(defender);

        if (!Roll(pm, family, difficulty))
        {
            return;
        }

        var found = unknown[Utility.Random(unknown.Count)];
        Learn(pm, defender, found.Location);

        var where = MahaonCombatChoices.LocationName(found.Location).ToLowerInvariant();
        var text = found.Multiplier > 1.0
            ? $"Ты замечаешь слабое место — {defender.Name}: {where}, урон ×{found.Multiplier:0.0}."
            : $"Ты замечаешь — {defender.Name}: {where} защищено крепче, урон ×{found.Multiplier:0.0}. Туда бить невыгодно.";

        if (found.Multiplier > 1.0 && !HitLocationSystem.CanUseLocation(pm, found.Location))
        {
            text += $" Целиться туда — нужна тактика {HitLocationSystem.TacticsRequirement(found.Location):0}.";
        }

        pm.SendMessage(MessageHue, text);
        MahaonCombatWindow.Push(pm);
    }

    /// <summary>Проверка навыка своего семейства; у смешанных — лучший из двух.</summary>
    private static bool Roll(PlayerMobile pm, Family family, double difficulty)
    {
        var min = difficulty - 30.0;
        var max = difficulty + 70.0;

        if (family.ByAnatomy && family.ByAnimalLore)
        {
            var skill = pm.Skills.Anatomy.Value >= pm.Skills.AnimalLore.Value ? SkillName.Anatomy : SkillName.AnimalLore;
            return pm.CheckSkill(skill, min, max);
        }

        return pm.CheckSkill(family.ByAnatomy ? SkillName.Anatomy : SkillName.AnimalLore, min, max);
    }

    /// <summary>
    ///     Игрок или человек: анатомия показывает слот, где броня хуже всего держит твоё оружие.
    ///     Знание не копится — броню переодевают.
    /// </summary>
    private static void ReadArmor(PlayerMobile pm, Mobile defender)
    {
        if (!pm.CheckSkill(SkillName.Anatomy, 20.0, 120.0))
        {
            return;
        }

        var best = HitLocationSystem.GetBestLocation(pm, defender);

        if (best == null)
        {
            return;
        }

        pm.SendMessage(
            MessageHue,
            $"Ты замечаешь — {defender.Name}: хуже всего прикрыто — {MahaonCombatChoices.LocationName(best.Value).ToLowerInvariant()}."
        );
    }

    // ---- В удар ---------------------------------------------------------------------------------

    /// <summary>
    ///     Вклад в урон за слабое или крепкое место. Зовётся из BaseWeapon.OnHit сразу после
    ///     расхода прицела — нужен именно тот прицел, что сработал в этом ударе.
    /// </summary>
    public static void AddToStrike(StrikeProfile strike, Mobile attacker, Mobile defender)
    {
        var location = HitLocationSystem.GetLastConsumedLocation(attacker);

        if (location == null)
        {
            return;
        }

        var family = FamilyOf(defender);

        if (family == null)
        {
            return;
        }

        foreach (var point in family.Points)
        {
            if (point.Location != location.Value)
            {
                continue;
            }

            // Слабое — только открытое; крепкое держит всегда.
            if (point.Multiplier > 1.0 && !IsKnown(attacker, defender, point.Location))
            {
                continue;
            }

            var label = point.Multiplier > 1.0 ? "Слабое место" : "Крепкое место";
            strike.Add($"{label}: ×{point.Multiplier:0.0}", StrikeField.Damage, Math.Round((point.Multiplier - 1.0) * 100));
        }
    }
}
