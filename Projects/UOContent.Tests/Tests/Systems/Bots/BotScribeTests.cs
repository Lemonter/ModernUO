using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotScribeTests
{
    [Fact]
    public void Scribe_LearnsSpellsIntoItsBook_ThenWritesThem_WhileManaLasts()
    {
        if (DefInscription.CraftSystem == null)
        {
            DefInscription.Initialize();
        }

        var bot = new BotMobile { Name = "Писарь", Player = true, Body = 0x190, RawInt = 80 };
        try
        {
            bot.AddItem(new Backpack());
            bot.Skills.Inscribe.Base = 60;
            bot.MoveToWorld(new Point3D(2500, 1800, 0), Map.Felucca);
            bot.Mana = bot.ManaMax;
            bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);

            var system = DefInscription.CraftSystem;
            Assert.True(BotScribe.IsScribe(bot));
            Assert.Equal(typeof(ScribesPen), BotCrafting.ToolTypeFor(system));
            Assert.Null(BotScribe.NextScrollToLearn(bot)); // no book yet

            bot.Backpack.DropItem(new ScribesPen());
            bot.Backpack.DropItem(new Spellbook());
            bot.Backpack.DropItem(new BlankScroll(20));
            bot.Backpack.DropItem(new Garlic(20));
            bot.Backpack.DropItem(new SpidersSilk(20));
            bot.Backpack.DropItem(new SulfurousAsh(20));
            bot.Backpack.DropItem(new Ginseng(20));

            Assert.True(BotCrafting.KeepsForCraft(bot, bot.Backpack.FindItemByType<BlankScroll>()));
            Assert.False(BotCrafting.TryPick(bot, system, out _)); // an empty book writes nothing

            var toLearn = BotScribe.NextScrollToLearn(bot);
            Assert.NotNull(toLearn);

            var bought = toLearn.CreateEntityInstance<SpellScroll>();
            bot.Backpack.DropItem(bought);
            Assert.True(BotCrafting.KeepsForCraft(bot, bought));

            Assert.Equal(1, BotScribe.FillBook(bot));
            Assert.True(BotScribe.KnowsSpell(bot, toLearn));
            Assert.NotEqual(toLearn, BotScribe.NextScrollToLearn(bot));

            // Fill the first circle so a scroll with the reagents at hand is known.
            foreach (var type in new[] { typeof(HealScroll), typeof(NightSightScroll), typeof(ReactiveArmorScroll) })
            {
                if (!BotScribe.KnowsSpell(bot, type))
                {
                    bot.Backpack.DropItem(type.CreateEntityInstance<SpellScroll>());
                }
            }

            BotScribe.FillBook(bot);
            Assert.True(BotCrafting.TryPick(bot, system, out var choice));
            Assert.True(BotScribe.KnowsSpell(bot, choice.Item.ItemType));

            bot.Mana = 0;
            Assert.False(BotCrafting.TryPick(bot, system, out _));
        }
        finally
        {
            bot.Delete();
        }
    }
}
