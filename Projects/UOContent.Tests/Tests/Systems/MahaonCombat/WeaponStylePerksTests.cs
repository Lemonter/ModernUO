using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Этап 8: стиль — плюшка, растущая с тренировкой, и постоянная цена.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class WeaponStylePerksTests
{
    private const byte Berserk = 3;
    private const byte Kenjutsu = 1;
    private const byte Duelist = 2;

    [Fact]
    public void EveryNamedStyle_HasATradeWithAPrice()
    {
        for (var c = WeaponCategory.Wrestling; c <= WeaponCategory.Throwing; c++)
        {
            Assert.Null(WeaponStylePerks.TradeOf(c, 0)); // классический — без плюшки и без цены

            for (byte s = 1; s < WeaponStyleSystem.StyleCount; s++)
            {
                var trade = WeaponStylePerks.TradeOf(c, s);
                Assert.NotNull(trade);

                var price = trade.Price;
                Assert.True(
                    price.Hit < 0 || price.Crit < 0 || price.Damage < 0 || price.Speed < 0 || price.Parry < 0 || price.Taken > 0,
                    $"{c}/{s}: у стиля нет цены"
                );
            }
        }
    }

    [Fact]
    public void Berserk_GainGrowsWithTraining_PriceDoesNot()
    {
        WeaponStylePerks.Register();

        var pm = new PlayerMobile();
        var sword = new Katana();

        try
        {
            WeaponStyleSystem.SetActiveStyle(pm, WeaponCategory.Swords, Berserk);

            // Новичок: пятая часть плюшки (+25% → +5%), цена целиком.
            var novice = StrikeProfiles.Gather(pm, null, sword);
            Assert.Contains(novice.Lines, l => l.Field == StrikeField.Damage && l.Value == 5 && l.Source == "Стиль: берсерк");
            Assert.Equal(-15, WeaponStylePerks.ParryModifier(pm, sword));
            Assert.Equal(1.10, WeaponStylePerks.DamageTakenScalar(pm, sword), 3);

            MahaonProgress.Set(pm, "branch:style/Swords/3", 100.0);

            var master = StrikeProfiles.Gather(pm, null, sword);
            Assert.Contains(master.Lines, l => l.Field == StrikeField.Damage && l.Value == 25 && l.Source == "Стиль: берсерк");
            Assert.Equal(-15, WeaponStylePerks.ParryModifier(pm, sword)); // цена не уменьшилась
        }
        finally
        {
            pm.Delete();
            sword.Delete();
        }
    }

    [Fact]
    public void Kenjutsu_IsFasterButLessAccurate()
    {
        WeaponStylePerks.Register();

        var pm = new PlayerMobile();
        var sword = new Katana();

        try
        {
            WeaponStyleSystem.SetActiveStyle(pm, WeaponCategory.Swords, Kenjutsu);
            MahaonProgress.Set(pm, "branch:style/Swords/1", 100.0);

            Assert.Equal(1.15, WeaponStylePerks.SpeedScalar(pm, sword), 3);
            Assert.Contains(
                StrikeProfiles.Gather(pm, null, sword).Lines,
                l => l.Field == StrikeField.HitChance && l.Value == -5
            );
        }
        finally
        {
            pm.Delete();
            sword.Delete();
        }
    }

    [Fact]
    public void Describe_ShowsGainAndPrice()
    {
        var pm = new PlayerMobile();

        try
        {
            var text = WeaponStylePerks.Describe(pm, WeaponCategory.Swords, Duelist);

            Assert.Contains("к парированию", text);
            Assert.Contains("цена:", text);
            Assert.Contains("урона", text);
            Assert.Equal("без плюшки и без цены", WeaponStylePerks.Describe(pm, WeaponCategory.Swords, 0));
        }
        finally
        {
            pm.Delete();
        }
    }
}
