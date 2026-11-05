using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

public enum CombatStance
{
    Normal,
    Defensive, // half damage dealt, half damage taken
    Aggressive2x, // «Агрессивная»: +25% скорости, −10% урона, −10 к попаданию (имя enum — историческое)
    Aggressive3x  // «Яростная»: +45% скорости, −15% урона, −15 к попаданию (имя enum — историческое)
}

/// <summary>
///     Mahaon tactics-linked combat stances. Defensive is available to everyone; the
///     aggressive stances require enough Tactics skill to unlock (mirrors the original
///     "115 tactics unlocks neck hits" progression — same skill, different payoff).
///
///     ---- Агрессивные стойки (05.11.2026) ----
///
///     Раньше были «×2» и «×3» скорости даром — выбор без цены, то есть не выбор. Теперь
///     быстрее, но каждый удар слабее и чаще мимо. Числа подобраны так, чтобы чистый урон в
///     секунду остался примерно прежним (±5% при попадании 70–95%), а выигрыш был в другом:
///     больше ударов — чаще срабатывают яды, зачарования, стили, сбивание каста, прирост
///     мастерства и разглядывание слабых мест. Яростная при этом наказывает неточных: при
///     попадании ниже ~65% она уже в минусе.
///
///     Штраф к попаданию — вкладом в профиль удара, поэтому он виден в чеке и в окне Unity.
/// </summary>
public class CombatStanceSystem : GenericPersistence
{
    private static CombatStanceSystem _instance;

    private static readonly Dictionary<Mobile, CombatStance> Stances = new();

    // Tune these against real playtesting — first-pass numbers.
    private const double Aggressive2xTacticsRequirement = 60.0;
    private const double Aggressive3xTacticsRequirement = 100.0;

    public const double AggressiveSpeed = 1.25;
    public const double AggressiveDamage = 0.90;
    public const int AggressiveHitPenalty = -10;

    public const double FuriousSpeed = 1.45;
    public const double FuriousDamage = 0.85;
    public const int FuriousHitPenalty = -15;

    public CombatStanceSystem() : base("MahaonCombatStances", 1)
    {
    }

    public static void Configure()
    {
        _instance = new CombatStanceSystem();
        RegisterHitPenalty();
    }

    /// <summary>Отдельно от Configure: тот заводит хранилище. Повторный вызов безвреден — для тестов.</summary>
    public static void RegisterHitPenalty() => StrikeProfiles.Register(HitPenalty);

    public static CombatStance GetStance(Mobile m) => Stances.GetValueOrDefault(m, CombatStance.Normal);

    public static double GetDamageDealtScalar(Mobile m) => GetStance(m) switch
    {
        CombatStance.Defensive    => 0.5,
        CombatStance.Aggressive2x => AggressiveDamage,
        CombatStance.Aggressive3x => FuriousDamage,
        _                         => 1.0
    };

    /// <summary>Штраф к попаданию в процентных пунктах (отрицательный).</summary>
    public static int GetHitModifier(Mobile m) => GetStance(m) switch
    {
        CombatStance.Aggressive2x => AggressiveHitPenalty,
        CombatStance.Aggressive3x => FuriousHitPenalty,
        _                         => 0
    };

    public static double GetDamageTakenScalar(Mobile m) =>
        GetStance(m) == CombatStance.Defensive ? 0.5 : 1.0;

    public static double GetSwingSpeedScalar(Mobile m) => GetStance(m) switch
    {
        CombatStance.Aggressive2x => AggressiveSpeed,
        CombatStance.Aggressive3x => FuriousSpeed,
        _                         => 1.0
    };

    public static bool CanUseStance(Mobile m, CombatStance stance) => stance switch
    {
        CombatStance.Aggressive2x => m.Skills[SkillName.Tactics].Value >= Aggressive2xTacticsRequirement,
        CombatStance.Aggressive3x => m.Skills[SkillName.Tactics].Value >= Aggressive3xTacticsRequirement,
        _                         => true
    };

    public static void SetStance(Mobile m, CombatStance stance)
    {
        if (stance == CombatStance.Normal)
        {
            Stances.Remove(m);
        }
        else
        {
            Stances[m] = stance;
        }
    }

    private static readonly HitPenaltyContributor HitPenalty = new();

    private sealed class HitPenaltyContributor : IStrikeContributor
    {
        public void Contribute(in StrikeContext context, StrikeProfile profile)
        {
            if (context.Attacker == null)
            {
                return;
            }

            profile.Add(
                $"Стойка: {MahaonCombatChoices.StanceName(GetStance(context.Attacker)).ToLowerInvariant()}",
                StrikeField.HitChance,
                GetHitModifier(context.Attacker)
            );
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Stances.Count);

        foreach (var (mobile, stance) in Stances)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt((int)stance);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var stance = (CombatStance)reader.ReadEncodedInt();

            if (mobile != null)
            {
                Stances[mobile] = stance;
            }
        }
    }
}
