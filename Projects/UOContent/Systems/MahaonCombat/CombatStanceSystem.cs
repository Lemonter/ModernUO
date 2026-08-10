using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

public enum CombatStance
{
    Normal,
    Defensive, // half damage dealt, half damage taken
    Aggressive2x, // normal damage, double swing speed, normal damage taken
    Aggressive3x // normal damage, triple swing speed, normal damage taken
}

/// <summary>
///     Mahaon tactics-linked combat stances. Defensive is available to everyone; the
///     aggressive stances require enough Tactics skill to unlock (mirrors the original
///     "115 tactics unlocks neck hits" progression — same skill, different payoff).
/// </summary>
public class CombatStanceSystem : GenericPersistence
{
    private static CombatStanceSystem _instance;

    private static readonly Dictionary<Mobile, CombatStance> Stances = new();

    // Tune these against real playtesting — first-pass numbers.
    private const double Aggressive2xTacticsRequirement = 60.0;
    private const double Aggressive3xTacticsRequirement = 100.0;

    public CombatStanceSystem() : base("MahaonCombatStances", 1)
    {
    }

    public static void Configure()
    {
        _instance = new CombatStanceSystem();
    }

    public static CombatStance GetStance(Mobile m) => Stances.GetValueOrDefault(m, CombatStance.Normal);

    public static double GetDamageDealtScalar(Mobile m) =>
        GetStance(m) == CombatStance.Defensive ? 0.5 : 1.0;

    public static double GetDamageTakenScalar(Mobile m) =>
        GetStance(m) == CombatStance.Defensive ? 0.5 : 1.0;

    public static double GetSwingSpeedScalar(Mobile m) => GetStance(m) switch
    {
        CombatStance.Aggressive2x => 2.0,
        CombatStance.Aggressive3x => 3.0,
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
