using Server.Items;
using Server.Mobiles;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Third;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

/// <summary>
///     Закрепляет то, из-за чего маги на кладбище фитилили без остановки: выбор заклинания
///     шёл по угаданным порогам навыка, и Lightning — заклинание ЧЕТВЁРТОГО круга — был
///     разрешён с магией 30, где его шанс всего 15%.
///
///     Тесты держат две вещи. Первую — что шанс считается по таблице движка, а не по
///     самодельной формуле: если MagerySpell._requiredSkill когда-нибудь поправят, числа
///     здесь разойдутся и это станет видно. Вторую — что при магии 30 четвёртый круг
///     недоступен, а третий на самой границе.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class BotSpellbookTests
{
    private static BotMobile NewMage(double magery)
    {
        var bot = new BotMobile { Name = "Маг", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());

        // Реагенты обязательны: теперь они настоящие и для ботов тоже (см. комментарий в
        // Spell.ConsumeReagents). Без них Usable честно отвечает «нет», что и выяснилось,
        // когда эти тесты упали в первый раз.
        bot.Backpack.DropItem(new MahaonReagentPouch());

        bot.Skills.Magery.Base = magery;
        bot.RawInt = 100;
        bot.Mana = bot.ManaMax;
        bot.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
        return bot;
    }

    /// <summary>
    ///     Пустой рюкзак — и самый надёжный удар недоступен. Проверка стоит здесь нарочно:
    ///     она фиксирует, что реагенты у ботов перестали быть бесплатными.
    /// </summary>
    [Fact]
    public void NoReagents_NoSpell()
    {
        var bot = new BotMobile { Name = "Нищий маг", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Magery.Base = 100;
        bot.RawInt = 100;
        bot.Mana = bot.ManaMax;
        bot.MoveToWorld(new Point3D(1500, 1601, 0), Map.Felucca);

        try
        {
            Assert.Equal(1.0, BotSpellbook.CastChance(bot, new MagicArrowSpell(bot)), 3);
            Assert.False(BotSpellbook.Usable(bot, new MagicArrowSpell(bot), 0));
        }
        finally
        {
            bot.Delete();
        }
    }

    /// <summary>
    ///     Таблица требований в ветке Core.ML: круг C (считая с единицы) берёт границы из
    ///     _requiredSkill[C + 1], ширина окна всегда 40. Отсюда и ожидания ниже.
    ///
    ///     круг 1: −18..22   круг 2: −4..36   круг 3: 10..50
    ///     круг 4: 24..64    круг 7: 66..106
    /// </summary>
    [Fact]
    public void CastChance_MatchesEngineSkillTable()
    {
        var bot = NewMage(30);

        try
        {
            // Первый круг при 30 — навык выше верхней границы, промаха быть не может.
            Assert.Equal(1.0, BotSpellbook.CastChance(bot, new MagicArrowSpell(bot)), 3);

            // Второй: (30 − (−4)) / 40 = 0.85
            Assert.Equal(0.85, BotSpellbook.CastChance(bot, new HarmSpell(bot)), 3);

            // Третий: (30 − 10) / 40 = 0.5
            Assert.Equal(0.5, BotSpellbook.CastChance(bot, new FireballSpell(bot)), 3);

            // Четвёртый: (30 − 24) / 40 = 0.15 — та самая цифра, из-за которой всё и затевалось.
            Assert.Equal(0.15, BotSpellbook.CastChance(bot, new LightningSpell(bot)), 3);

            // Седьмой при 30 — ниже нижней границы, шанса нет вовсе.
            Assert.Equal(0.0, BotSpellbook.CastChance(bot, new FlameStrikeSpell(bot)), 3);
        }
        finally
        {
            bot.Delete();
        }
    }

    /// <summary>Главное следствие: магу с навыком 30 четвёртый круг не положен, третий — да.</summary>
    [Fact]
    public void WeakMage_StopsAtThirdCircle()
    {
        var bot = NewMage(30);

        try
        {
            Assert.True(BotSpellbook.Usable(bot, new MagicArrowSpell(bot), 0));
            Assert.True(BotSpellbook.Usable(bot, new HarmSpell(bot), 0));

            // Ровно на пороге: третий круг при 30 даёт те самые 50%.
            Assert.True(BotSpellbook.Usable(bot, new FireballSpell(bot), 0));

            Assert.False(BotSpellbook.Usable(bot, new LightningSpell(bot), 0));
            Assert.False(BotSpellbook.Usable(bot, new FlameStrikeSpell(bot), 0));
        }
        finally
        {
            bot.Delete();
        }
    }

    /// <summary>А сильному магу — положен, иначе проверка выше запрещала бы вообще всё.</summary>
    [Fact]
    public void StrongMage_ReachesTopCircles()
    {
        var bot = NewMage(100);

        try
        {
            Assert.True(BotSpellbook.Usable(bot, new LightningSpell(bot), 0));

            // Седьмой круг при 100: (100 − 66) / 40 = 0.85, выше порога.
            Assert.True(BotSpellbook.Usable(bot, new FlameStrikeSpell(bot), 0));
        }
        finally
        {
            bot.Delete();
        }
    }

    /// <summary>
    ///     Нет маны с запасом — нет и заклинания, каким бы надёжным оно ни было. Это та самая
    ///     защита, без которой маг выливал последнюю ману в атаку и оставался без лечения.
    /// </summary>
    [Fact]
    public void ManaReserve_BlocksTheLastDrop()
    {
        var bot = NewMage(100);

        try
        {
            var bolt = new LightningSpell(bot);
            var cost = bolt.GetMana();

            bot.Mana = cost;
            Assert.True(BotSpellbook.Usable(bot, new LightningSpell(bot), 0));
            Assert.False(BotSpellbook.Usable(bot, new LightningSpell(bot), 11));

            bot.Mana = cost - 1;
            Assert.False(BotSpellbook.Usable(bot, new LightningSpell(bot), 0));
        }
        finally
        {
            bot.Delete();
        }
    }

    /// <summary>
    ///     Ожидаемая польза, а не грубая сила: при магии 30 третий круг с шансом 50% не должен
    ///     обгонять второй с шансом 85%, если на ману он при этом дороже. Это и есть то
    ///     сравнение, которым бот выбирает удар.
    /// </summary>
    [Fact]
    public void Value_WeighsChanceAndMana_NotRawPower()
    {
        var bot = NewMage(30);

        try
        {
            // Силы заданы так же, как в BotCombat: Harm 9, Fireball 14.
            var harm = BotSpellbook.Value(bot, new HarmSpell(bot), 9);
            var fireball = BotSpellbook.Value(bot, new FireballSpell(bot), 14);

            // Harm: 9 * 0.85 / 6 = 1.275. Fireball: 14 * 0.5 / 9 = 0.778.
            Assert.True(harm > fireball, $"ожидали, что Harm выгоднее: {harm:F3} против {fireball:F3}");

            // У сильного мага всё наоборот — иначе он навсегда остался бы на втором круге.
            var strong = NewMage(100);

            try
            {
                var strongHarm = BotSpellbook.Value(strong, new HarmSpell(strong), 9);
                var strongFireball = BotSpellbook.Value(strong, new FireballSpell(strong), 14);

                Assert.True(
                    strongFireball > strongHarm,
                    $"ожидали, что Fireball выгоднее: {strongFireball:F3} против {strongHarm:F3}"
                );
            }
            finally
            {
                strong.Delete();
            }
        }
        finally
        {
            bot.Delete();
        }
    }
}
