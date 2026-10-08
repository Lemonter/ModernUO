using System;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Mahaon: everything the three Necromancy summons (AnimateDead, SummonFamiliar,
///     VengefulSpirit) take from the "Школа призыва" branch of NecromancySchoolSystem.
///
///     Vanilla scales those spells off SpiritSpeak alone and caps them at flat numbers
///     (3 raised undead, 1 familiar, 1 revenant), which left the summoning mastery with
///     nothing to do but shave mana. This is the one place that turns mastery into
///     head-count, lifetime and raw power, so all three spells stay in step with each
///     other and there is a single set of numbers to retune.
/// </summary>
public static class NecromancySummonSystem
{
    /// <summary>Raised undead lose 1 HP per tick; this is the vanilla tick length that
    /// mastery stretches. See AnimateDeadSpell.Register.</summary>
    public const int BaseDecayMilliseconds = 1650;

    private const double MaxDecayMultiplier = 4.0;  // 1650 ms -> 6600 ms at 100 mastery
    private const double MaxPowerBonus = 0.60;      // +60% hits/stats/damage at 100 mastery

    public static double GetMastery(Mobile m) =>
        m == null ? 0.0 : NecromancySchoolSystem.GetValue(m, NecromancySchool.Summoning);

    /// <summary>Raised undead allowed at once — one per 20 Necromancy, never below one.
    /// At GM Necromancy that is five.</summary>
    public static int GetAnimateLimit(Mobile m) =>
        m == null ? 1 : Math.Max(1, (int)(m.Skills.Necromancy.Value / 20.0));

    /// <summary>Familiars allowed at once — one, plus one per 50 mastery.</summary>
    public static int GetFamiliarLimit(Mobile m) => 1 + (int)(GetMastery(m) / 50.0);

    /// <summary>Revenants allowed at once — one, plus one per 30 mastery.</summary>
    public static int GetVengefulLimit(Mobile m) => 1 + (int)(GetMastery(m) / 30.0);

    /// <summary>How long a raised undead takes to lose one hit point. Mastery stretches
    /// the vanilla 1.65 s up to 6.6 s, i.e. four times the lifetime.</summary>
    public static TimeSpan GetDecayInterval(Mobile m)
    {
        var interval = BaseDecayMilliseconds * (1.0 + GetMastery(m) / 100.0 * (MaxDecayMultiplier - 1.0));

        // «Власть над мёртвыми» — сигнатурный перк категории Некромантия: поднятая нежить
        // живёт вдвое дольше. Складывается с бонусом от Школы призыва, а не заменяет его.
        if (Systems.MahaonProfessions.ProfessionSystem.HasSignature(
                m, Systems.MahaonProfessions.ProfessionCategory.Necromancy
            ))
        {
            interval *= 2.0;
        }

        return TimeSpan.FromMilliseconds(interval);
    }

    /// <summary>Multiplier applied to a summon's hit points, stats and damage.</summary>
    public static double GetPowerScalar(Mobile m) => 1.0 + GetMastery(m) / 100.0 * MaxPowerBonus;

    /// <summary>
    ///     Applied right after a summon is created, before it is dropped into the world.
    ///     Only touches fields the creature actually defines — a creature that leaves
    ///     DamageMin/Max at -1 fights with its weapon's damage, and multiplying -1 would
    ///     turn that into nonsense.
    /// </summary>
    public static void ApplyMasteryPower(Mobile caster, BaseCreature bc)
    {
        if (caster == null || bc == null)
        {
            return;
        }

        var scalar = GetPowerScalar(caster);

        // «Костяной доспех» — обычный перк категории Некромантия: поднятая им нежить
        // держится в полтора раза дольше под ударами. Множится с бонусом Школы призыва,
        // а не заменяет его.
        if (Systems.MahaonProfessions.ProfessionSystem.HasFullKit(
                caster, Systems.MahaonProfessions.ProfessionCategory.Necromancy
            ))
        {
            scalar *= 1.5;
        }

        if (scalar <= 1.0)
        {
            return;
        }

        if (bc.RawStr > 0)
        {
            bc.RawStr = Scale(bc.RawStr, scalar);
        }

        if (bc.RawDex > 0)
        {
            bc.RawDex = Scale(bc.RawDex, scalar);
        }

        if (bc.RawInt > 0)
        {
            bc.RawInt = Scale(bc.RawInt, scalar);
        }

        if (bc.HitsMaxSeed > 0)
        {
            bc.HitsMaxSeed = Scale(bc.HitsMaxSeed, scalar);
        }

        if (bc.DamageMin > 0)
        {
            bc.DamageMin = Scale(bc.DamageMin, scalar);
        }

        if (bc.DamageMax > 0)
        {
            bc.DamageMax = Scale(bc.DamageMax, scalar);
        }

        bc.Hits = bc.HitsMax;
        bc.Stam = bc.StamMax;
        bc.Mana = bc.ManaMax;
    }

    private static int Scale(int value, double scalar) => Math.Max(1, (int)(value * scalar));

    /// <summary>Overhead line so the player can see the mastery is doing something —
    /// silent below 10, where the numbers would round to nothing anyway.</summary>
    public static void AnnounceSummon(Mobile caster, BaseCreature bc, int current, int limit)
    {
        var mastery = GetMastery(caster);

        if (mastery < 10.0 || bc == null)
        {
            return;
        }

        bc.PublicOverheadMessage(
            MessageType.Regular,
            0x3B2,
            false,
            $"Школа призыва {mastery:F0} — {current}/{limit}"
        );
    }
}
