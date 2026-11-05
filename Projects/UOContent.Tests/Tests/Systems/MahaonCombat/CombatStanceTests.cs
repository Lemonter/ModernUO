using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Агрессивные стойки больше не дают скорость даром: быстрее, но слабее и чаще мимо.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class CombatStanceTests
{
    [Fact]
    public void Aggressive_IsFasterButWeakerAndLessAccurate()
    {
        var pm = new PlayerMobile();

        try
        {
            CombatStanceSystem.SetStance(pm, CombatStance.Aggressive2x);

            Assert.Equal(1.25, CombatStanceSystem.GetSwingSpeedScalar(pm));
            Assert.Equal(0.90, CombatStanceSystem.GetDamageDealtScalar(pm));
            Assert.Equal(-10, CombatStanceSystem.GetHitModifier(pm));

            CombatStanceSystem.SetStance(pm, CombatStance.Aggressive3x);

            Assert.Equal(1.45, CombatStanceSystem.GetSwingSpeedScalar(pm));
            Assert.Equal(0.85, CombatStanceSystem.GetDamageDealtScalar(pm));
            Assert.Equal(-15, CombatStanceSystem.GetHitModifier(pm));
        }
        finally
        {
            CombatStanceSystem.SetStance(pm, CombatStance.Normal);
            pm.Delete();
        }
    }

    /// <summary>Урон в секунду почти не меняется — стойка про число ударов, а не про чистый урон.</summary>
    [Theory]
    [InlineData(0.70)]
    [InlineData(0.85)]
    [InlineData(0.95)]
    public void Dps_StaysRoughlyTheSame(double baseHit)
    {
        var aggressive = CombatStanceSystem.AggressiveSpeed * CombatStanceSystem.AggressiveDamage *
                         (baseHit + CombatStanceSystem.AggressiveHitPenalty / 100.0) / baseHit;
        var furious = CombatStanceSystem.FuriousSpeed * CombatStanceSystem.FuriousDamage *
                      (baseHit + CombatStanceSystem.FuriousHitPenalty / 100.0) / baseHit;

        Assert.InRange(aggressive, 0.95, 1.05);
        Assert.InRange(furious, 0.95, 1.06);
    }

    [Fact]
    public void HitPenalty_ShowsInTheStrikeProfile()
    {
        CombatStanceSystem.RegisterHitPenalty();

        var pm = new PlayerMobile();
        var sword = new Katana();

        try
        {
            Assert.DoesNotContain(StrikeProfiles.Gather(pm, null, sword).Lines, l => l.Source.StartsWith("Стойка: агрессивная"));

            CombatStanceSystem.SetStance(pm, CombatStance.Aggressive2x);

            Assert.Contains(
                StrikeProfiles.Gather(pm, null, sword).Lines,
                l => l.Field == StrikeField.HitChance && l.Value == -10 && l.Source == "Стойка: агрессивная"
            );
        }
        finally
        {
            CombatStanceSystem.SetStance(pm, CombatStance.Normal);
            pm.Delete();
            sword.Delete();
        }
    }

    [Fact]
    public void Defensive_IsUnchanged()
    {
        var pm = new PlayerMobile();

        try
        {
            CombatStanceSystem.SetStance(pm, CombatStance.Defensive);

            Assert.Equal(1.0, CombatStanceSystem.GetSwingSpeedScalar(pm));
            Assert.Equal(0.5, CombatStanceSystem.GetDamageDealtScalar(pm));
            Assert.Equal(0.5, CombatStanceSystem.GetDamageTakenScalar(pm));
            Assert.Equal(0, CombatStanceSystem.GetHitModifier(pm));
        }
        finally
        {
            CombatStanceSystem.SetStance(pm, CombatStance.Normal);
            pm.Delete();
        }
    }
}
