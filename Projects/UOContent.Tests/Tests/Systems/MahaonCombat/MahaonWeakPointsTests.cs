using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Этап 5: место удара как компромисс и слабые места существ.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonWeakPointsTests
{
    // ---- Цена прицела ----------------------------------------------------------------------

    [Fact]
    public void Aim_CostsHitChance_TheSmallerTheTargetTheMore()
    {
        Assert.Equal(0, HitLocationSystem.HitModifier(HitLocation.Chest));
        Assert.True(HitLocationSystem.HitModifier(HitLocation.Neck) < HitLocationSystem.HitModifier(HitLocation.Head));
        Assert.True(HitLocationSystem.HitModifier(HitLocation.Head) < HitLocationSystem.HitModifier(HitLocation.Arms));
    }

    [Fact]
    public void Aim_ReachesTheStrikeProfileAsAPenalty()
    {
        HitLocationSystem.RegisterAimCost();

        var pm = new PlayerMobile();
        var halberd = new Server.Items.Halberd();

        try
        {
            pm.Skills.Tactics.Base = 120.0;
            HitLocationSystem.SetLocation(pm, HitLocation.Neck);

            var profile = StrikeProfiles.Gather(pm, null, halberd);

            Assert.Contains(profile.Lines, l => l.Field == StrikeField.HitChance && l.Value == -20 && l.Source.Contains("шея"));
        }
        finally
        {
            halberd.Delete();
            pm.Delete();
        }
    }

    /// <summary>Целиться в спину тому, кто стоит к тебе лицом, нельзя.</summary>
    [Fact]
    public void Back_OnlyFromBehind()
    {
        var attacker = new Mobile();
        var defender = new Mobile();

        try
        {
            defender.Location = new Point3D(100, 100, 0);
            defender.Direction = Direction.North;

            attacker.Location = new Point3D(100, 101, 0); // южнее — то есть за спиной смотрящего на север
            Assert.True(HitLocationSystem.IsBehind(attacker, defender));

            attacker.Location = new Point3D(100, 99, 0);  // севернее — прямо перед глазами
            Assert.False(HitLocationSystem.IsBehind(attacker, defender));

            attacker.Location = new Point3D(101, 101, 0); // сзади-сбоку — ещё считается спиной
            Assert.True(HitLocationSystem.IsBehind(attacker, defender));
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
        }
    }

    // ---- Семейства --------------------------------------------------------------------------

    /// <summary>Дракон — тоже рептилия, но драконья строка обязана сработать первой.</summary>
    [Fact]
    public void Dragon_IsADragonNotJustAReptile()
    {
        var dragon = new Dragon();

        try
        {
            Assert.Equal("dragons", MahaonWeakPoints.FamilyOf(dragon)?.Key);
        }
        finally
        {
            dragon.Delete();
        }
    }

    [Fact]
    public void Families_ComeFromSlayerGroups()
    {
        var skeleton = new Skeleton();
        var orc = new Orc();
        var spider = new GiantSpider();

        try
        {
            Assert.Equal("undead", MahaonWeakPoints.FamilyOf(skeleton)?.Key);
            Assert.Equal("humanoids", MahaonWeakPoints.FamilyOf(orc)?.Key);
            Assert.Equal("arachnids", MahaonWeakPoints.FamilyOf(spider)?.Key);
        }
        finally
        {
            skeleton.Delete();
            orc.Delete();
            spider.Delete();
        }
    }

    /// <summary>Игрок — «в броне»: своей таблицы слабостей у него нет, слабость — худший слот.</summary>
    [Fact]
    public void Player_HasNoFixedWeakness()
    {
        var pm = new PlayerMobile();

        try
        {
            Assert.True(MahaonWeakPoints.IsArmored(pm));
            Assert.Null(MahaonWeakPoints.FamilyOf(pm));
        }
        finally
        {
            pm.Delete();
        }
    }

    // ---- Знание -------------------------------------------------------------------------------

    /// <summary>Знание — за видом существа: разобрался с одним драконом — знаешь всех.</summary>
    [Fact]
    public void Knowledge_IsPerCreatureKind()
    {
        var pm = new PlayerMobile();
        var first = new Dragon();
        var second = new Dragon();

        try
        {
            Assert.False(MahaonWeakPoints.IsKnown(pm, first, HitLocation.Neck));

            MahaonWeakPoints.Learn(pm, first, HitLocation.Neck);

            Assert.True(MahaonWeakPoints.IsKnown(pm, second, HitLocation.Neck));
            Assert.Single(MahaonWeakPoints.Known(pm, second));
        }
        finally
        {
            pm.Delete();
            first.Delete();
            second.Delete();
        }
    }

    // ---- В удар ---------------------------------------------------------------------------------

    /// <summary>
    ///     Слабое место бьёт сильнее только открытое; крепкое держит удар всегда — знание
    ///     нужно, чтобы туда не бить.
    /// </summary>
    [Fact]
    public void WeakPoint_CountsOnlyWhenKnown()
    {
        var pm = new PlayerMobile();
        var dragon = new Dragon();

        try
        {
            pm.Skills.Tactics.Base = 120.0;
            HitLocationSystem.SetLocation(pm, HitLocation.Neck);
            HitLocationSystem.ConsumeHitBonus(pm, dragon);

            var blind = new StrikeProfile();
            MahaonWeakPoints.AddToStrike(blind, pm, dragon);
            Assert.Empty(blind.Lines);

            MahaonWeakPoints.Learn(pm, dragon, HitLocation.Neck);

            var seeing = new StrikeProfile();
            MahaonWeakPoints.AddToStrike(seeing, pm, dragon);
            Assert.Equal(80, seeing.Sum(StrikeField.Damage)); // ×1.8
        }
        finally
        {
            pm.Delete();
            dragon.Delete();
        }
    }

    [Fact]
    public void StrongPlace_HoldsEvenUnknown()
    {
        var pm = new PlayerMobile();
        var dragon = new Dragon();

        try
        {
            pm.Skills.Tactics.Base = 120.0;

            dragon.Location = new Point3D(200, 200, 0);
            dragon.Direction = Direction.North;
            pm.Location = new Point3D(200, 201, 0); // за спиной

            HitLocationSystem.SetLocation(pm, HitLocation.Back);
            HitLocationSystem.ConsumeHitBonus(pm, dragon);

            var profile = new StrikeProfile();
            MahaonWeakPoints.AddToStrike(profile, pm, dragon);

            Assert.Equal(-30, profile.Sum(StrikeField.Damage)); // ×0.7, хоть игрок о нём и не знал
        }
        finally
        {
            pm.Delete();
            dragon.Delete();
        }
    }
}
