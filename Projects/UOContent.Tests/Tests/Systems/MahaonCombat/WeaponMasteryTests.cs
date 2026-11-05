using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Мастерство владения оружием (как в Albion): что качается, как течёт по линейке и что
///     отдаёт в удар.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class WeaponMasteryTests
{
    private static WeaponMasterySystem.Kind KindOf(BaseWeapon weapon)
    {
        Assert.True(WeaponMasterySystem.TryGetKind(weapon, out var kind));
        return kind;
    }

    [Fact]
    public void Halberd_IsAPolearm()
    {
        var halberd = new Halberd();

        try
        {
            var kind = KindOf(halberd);

            Assert.Equal("polearms", kind.Line);
            Assert.Equal("алебарда", kind.Name);
            Assert.Equal("mastery:weapon/Halberd", kind.Track);
        }
        finally
        {
            halberd.Delete();
        }
    }

    /// <summary>Артефакт качает своё базовое оружие: Calm наследует Halberd.</summary>
    [Fact]
    public void Artifact_TrainsItsBaseWeapon()
    {
        var calm = new Calm();
        var halberd = new Halberd();

        try
        {
            Assert.Same(KindOf(halberd), KindOf(calm));
        }
        finally
        {
            calm.Delete();
            halberd.Delete();
        }
    }

    [Fact]
    public void TribalSpear_IsJustASpear()
    {
        var tribal = new TribalSpear();
        var spear = new Spear();

        try
        {
            Assert.Same(KindOf(spear), KindOf(tribal));
        }
        finally
        {
            tribal.Delete();
            spear.Delete();
        }
    }

    /// <summary>
    ///     Копьё для верховых в коде наследует BaseSword, но по смыслу это копьё. Линейки нарочно
    ///     идут не только по базовому классу.
    /// </summary>
    [Fact]
    public void Lance_IsASpearNotASword()
    {
        var lance = new Lance();

        try
        {
            Assert.Equal("spears", KindOf(lance).Line);
        }
        finally
        {
            lance.Delete();
        }
    }

    [Fact]
    public void BowsAndCrossbows_AreSeparateLines()
    {
        var bow = new Bow();
        var crossbow = new Crossbow();

        try
        {
            Assert.Equal("bows", KindOf(bow).Line);
            Assert.Equal("crossbows", KindOf(crossbow).Line);
        }
        finally
        {
            bow.Delete();
            crossbow.Delete();
        }
    }

    /// <summary>
    ///     У каждого вида есть имя в творительном падеже — оно идёт в каждое сообщение о приросте,
    ///     и пустое место там было бы видно сразу. И каждая линейка, на которую ссылается вид,
    ///     существует.
    /// </summary>
    [Fact]
    public void EveryKind_HasANameForTheMessageAndAKnownLine()
    {
        foreach (var line in WeaponMasterySystem.Lines.Keys)
        {
            foreach (var kind in WeaponMasterySystem.KindsInLine(line))
            {
                Assert.False(string.IsNullOrWhiteSpace(kind.Name), kind.Key);
                Assert.False(string.IsNullOrWhiteSpace(kind.Instrumental), kind.Key);
                Assert.True(WeaponMasterySystem.Lines.ContainsKey(kind.Line), kind.Key);
            }
        }

        // Ни одной пары одинаковых имён в одной линейке — иначе на доске два одинаковых пункта.
        foreach (var line in WeaponMasterySystem.Lines.Keys)
        {
            var names = WeaponMasterySystem.KindsInLine(line).Select(k => k.Name).ToList();
            Assert.Equal(names.Count, names.Distinct().Count());
        }
    }

    [Fact]
    public void Gain_FollowsTheSameBandsAsBranches()
    {
        Assert.Equal(0.3, WeaponMasterySystem.GainFor(10));
        Assert.Equal(0.15, WeaponMasterySystem.GainFor(60));
        Assert.Equal(0.05, WeaponMasterySystem.GainFor(90));
    }

    /// <summary>Четверть лучшего соседа по линейке — мастер алебарды берёт бердыш не с нуля.</summary>
    [Fact]
    public void Effective_AddsAQuarterOfTheBestNeighbour()
    {
        var m = new PlayerMobile();
        var halberd = new Halberd();
        var bardiche = new Bardiche();

        try
        {
            var h = KindOf(halberd);
            var b = KindOf(bardiche);

            MahaonProgress.Set(m, h.Track, 80);
            MahaonProgress.Set(m, b.Track, 0);

            Assert.Equal(20, WeaponMasterySystem.Effective(m, b));
            Assert.Equal(80, WeaponMasterySystem.Effective(m, h)); // соседи по нулям — только своё

            MahaonProgress.Set(m, b.Track, 90);

            Assert.Equal(WeaponMasterySystem.MaxLevel, WeaponMasterySystem.Effective(m, b)); // 90 + 20 → потолок
        }
        finally
        {
            halberd.Delete();
            bardiche.Delete();
            m.Delete();
        }
    }

    [Fact]
    public void Advance_StopsAtTheCap()
    {
        var m = new PlayerMobile();
        var halberd = new Halberd();

        try
        {
            var kind = KindOf(halberd);

            WeaponMasterySystem.Advance(m, kind, 99.98, 0.05);

            Assert.Equal(WeaponMasterySystem.MaxLevel, WeaponMasterySystem.Raw(m, kind));
            Assert.Equal(WeaponMasterySystem.MaxLevel, WeaponMasterySystem.Level(m, kind));
        }
        finally
        {
            halberd.Delete();
            m.Delete();
        }
    }

    /// <summary>
    ///     Отдача приходит в удар вкладом с подписью: попадание, крит и немного урона —
    ///     ровно по уровню.
    /// </summary>
    [Fact]
    public void Mastery_ReachesTheStrikeProfile()
    {
        WeaponMasterySystem.Configure();

        var m = new PlayerMobile();
        var halberd = new Halberd();

        try
        {
            MahaonProgress.Set(m, KindOf(halberd).Track, 50);

            var profile = StrikeProfiles.Gather(m, null, halberd);

            Assert.Equal(50 * WeaponMasterySystem.HitPerLevel, profile.Sum(StrikeField.HitChance), 6);
            Assert.Equal(50 * WeaponMasterySystem.CritPerLevel, profile.Sum(StrikeField.CritChance), 6);
            Assert.Equal(50 * WeaponMasterySystem.DamagePerLevel, profile.Sum(StrikeField.Damage), 6);
            Assert.All(profile.Lines, line => Assert.StartsWith("Мастерство: алебарда", line.Source));
        }
        finally
        {
            halberd.Delete();
            m.Delete();
        }
    }

    /// <summary>Монстрам мастерство не положено — у них и прокачки нет.</summary>
    [Fact]
    public void NonPlayers_GetNothing()
    {
        WeaponMasterySystem.Configure();

        var m = new Mobile();
        var halberd = new Halberd();

        try
        {
            Assert.Empty(StrikeProfiles.Gather(m, null, halberd).Lines);
        }
        finally
        {
            halberd.Delete();
            m.Delete();
        }
    }
}
