using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Этап 6: стойки лучника. Прежнее автоматическое правило «лук выше 90 — вдвое дальше, но
///     медленнее» стало одной из стоек; кто ничего не выбирал, получает ровно то, что было.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class ArcheryStanceTests
{
    /// <summary>Обновление ничего не отбирает: без выбора всё как раньше.</summary>
    [Fact]
    public void NoChoice_BehavesAsBefore()
    {
        var master = new PlayerMobile();
        var novice = new PlayerMobile();
        var bow = new Bow();

        try
        {
            master.Skills.Archery.Base = 95.0;
            novice.Skills.Archery.Base = 80.0;

            Assert.Equal(ArcheryStance.Lofted, ArcheryStanceSystem.Active(master));
            Assert.Equal(bow.MaxRange * 2, ArcheryStanceSystem.CombatRange(master, bow));

            Assert.Equal(ArcheryStance.Direct, ArcheryStanceSystem.Active(novice));
            Assert.Equal(bow.MaxRange, ArcheryStanceSystem.CombatRange(novice, bow));
        }
        finally
        {
            master.Delete();
            novice.Delete();
            bow.Delete();
        }
    }

    [Fact]
    public void Lofted_NeedsArcheryAbove90()
    {
        var pm = new PlayerMobile();

        try
        {
            pm.Skills.Archery.Base = 85.0;
            Assert.False(ArcheryStanceSystem.Set(pm, ArcheryStance.Lofted));

            pm.Skills.Archery.Base = 91.0;
            Assert.True(ArcheryStanceSystem.Set(pm, ArcheryStance.Lofted));

            // Навык упал ниже порога — стойка не держится, выстрел становится прямым.
            pm.Skills.Archery.Base = 85.0;
            Assert.Equal(ArcheryStance.Direct, ArcheryStanceSystem.Active(pm));
        }
        finally
        {
            pm.Delete();
        }
    }

    [Fact]
    public void Rapid_ShootsCloserAndFaster()
    {
        var pm = new PlayerMobile();
        var bow = new Bow();

        try
        {
            Assert.True(ArcheryStanceSystem.Set(pm, ArcheryStance.Rapid));

            Assert.Equal(bow.MaxRange - 2, ArcheryStanceSystem.CombatRange(pm, bow));
            Assert.True(ArcheryStanceSystem.DelayFactor(pm, bow) < 1.0);
        }
        finally
        {
            pm.Delete();
            bow.Delete();
        }
    }

    /// <summary>Прицельный: медленнее, зато точнее и чаще крит — вкладом в профиль.</summary>
    [Fact]
    public void Aimed_TradesSpeedForAccuracy()
    {
        ArcheryStanceSystem.Register();

        var pm = new PlayerMobile();
        var bow = new Bow();

        try
        {
            Assert.True(ArcheryStanceSystem.Set(pm, ArcheryStance.Aimed));

            Assert.True(ArcheryStanceSystem.DelayFactor(pm, bow) > 1.0);

            var profile = StrikeProfiles.Gather(pm, null, bow);

            Assert.Contains(profile.Lines, l => l.Field == StrikeField.HitChance && l.Value == 15);
            Assert.Contains(profile.Lines, l => l.Field == StrikeField.CritChance && l.Value == 5);
        }
        finally
        {
            pm.Delete();
            bow.Delete();
        }
    }

    /// <summary>Стойки лучника — только для луков и арбалетов: меч они не трогают.</summary>
    [Fact]
    public void MeleeWeapon_IsUntouched()
    {
        ArcheryStanceSystem.Register();

        var pm = new PlayerMobile();
        var sword = new Katana();

        try
        {
            ArcheryStanceSystem.Set(pm, ArcheryStance.Aimed);

            Assert.Equal(sword.MaxRange, ArcheryStanceSystem.CombatRange(pm, sword));
            Assert.Equal(1.0, ArcheryStanceSystem.DelayFactor(pm, sword));
            Assert.DoesNotContain(
                StrikeProfiles.Gather(pm, null, sword).Lines,
                l => l.Source.StartsWith("Стойка:")
            );
        }
        finally
        {
            pm.Delete();
            sword.Delete();
        }
    }

    /// <summary>Ядро спрашивает дальность у оружия — и получает её с учётом стойки.</summary>
    [Fact]
    public void Weapon_AnswersTheCoreWithTheStanceRange()
    {
        var pm = new PlayerMobile();
        var bow = new Bow();

        try
        {
            pm.Skills.Archery.Base = 100.0;
            ArcheryStanceSystem.Set(pm, ArcheryStance.Lofted);

            Assert.Equal(bow.MaxRange * 2, ((IWeapon)bow).GetCombatRange(pm));
        }
        finally
        {
            pm.Delete();
            bow.Delete();
        }
    }
}
