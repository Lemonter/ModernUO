using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonMetals;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class CityGuardUpgradeTests
{
    private static BotMobile NewBot(Point3D at)
    {
        var bot = new BotMobile { Name = "Стражелюб", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null) { Greed = 20 };
        BotGuilds.Join("ТестСтража", bot);
        return bot;
    }

    [Fact]
    public void Members_FundTheGuard_LeaderRaisesItAndHiresMages_NewHolderStartsOver()
    {
        if (!CommandSystem.Entries.ContainsKey("SetCityCenter"))
        {
            CityControlSystem.Configure();
        }

        const string city = "Trinsic";
        var (center, map) = CityControlSystem.Cities[city];
        var leader = NewBot(center);
        var member = NewBot(new Point3D(center.X + 1, center.Y, center.Z));
        try
        {
            var guild = (Guild)leader.Guild;
            CityControlSystem.Capture(city, guild);
            Assert.Equal(0, CityControlSystem.GetGuardLevel(city));
            Assert.Equal(0, BotGoals.CityGuardUpgrade.Score(leader.Brain)); // an empty bank buys nothing

            // The member gives the ingots the first step needs, then a share of its gold.
            member.Backpack.DropItem(new MahaonIngot(MahaonMetal.Iron, 1000));
            member.Backpack.DropItem(new Gold(60000));
            Assert.True(BotGoals.GuildDonate.Score(member.Brain) > 0.35);
            Assert.Equal(BotActionStatus.Done, new GuildDonateAction().Tick(member.Brain).Status);
            Assert.Equal(1000, GuildBank.GetIngots(guild.Name, MahaonMetal.Iron));

            member.Brain.NextDonationTick = Core.TickCount;
            Assert.Equal(BotActionStatus.Done, new GuildDonateAction().Tick(member.Brain).Status);
            Assert.Equal(54000, member.Backpack.GetAmount(typeof(Gold)));
            Assert.True(GuildBank.GetGoldValue(guild.Name) >= 6000);
            GuildBank.DepositGold(guild.Name, 4000);

            // Gold and ingots in the bank: the leader raises the guard, whose gear is forged of iron.
            Assert.Equal(0.6, BotGoals.CityGuardUpgrade.Score(leader.Brain));
            var guard = new List<CityGuard>(CityGuard.Of(city))[0];
            var hitsBefore = guard.HitsMax;
            Assert.Equal(BotActionStatus.Done, new CityGuardCommandAction(city, false).Tick(leader.Brain).Status);
            Assert.Equal(1, CityControlSystem.GetGuardLevel(city));
            Assert.Equal(0, GuildBank.GetIngots(guild.Name, MahaonMetal.Iron));
            Assert.True(guard.HitsMax > hitsBefore * 1.0);
            var chest = guard.Items.Find(item => item is PlateChest);
            Assert.NotNull(chest);
            Assert.Equal(MahaonMetal.Iron, MahaonMetalTracker.GetMetal(chest));

            // Gold beyond the next step's: a battle mage.
            GuildBank.DepositGold(guild.Name, 50000);
            Assert.Equal(BotActionStatus.Done, new CityGuardCommandAction(city, true).Tick(leader.Brain).Status);
            Assert.Equal(1, CityControlSystem.MagesIn(city));

            // A second step forges the gear in cobalt, coloured as the metal is.
            Assert.Equal(24, CityControlSystem.GuardSteps.Length);
            Assert.Equal(MahaonMetal.Lemium, CityControlSystem.GuardSteps[^1].Metal);
            GuildBank.DepositGold(guild.Name, CityControlSystem.GuardSteps[1].Gold);
            leader.Backpack.DropItem(new MahaonIngot(MahaonMetal.Cobalt, CityControlSystem.GuardSteps[1].Ingots));
            GuildBank.Deposit(guild.Name, leader.Backpack.FindItemByType<MahaonIngot>());
            Assert.True(CityControlSystem.UpgradeGuards(city, guild));
            var cobaltHue = MahaonMetalTable.Get(MahaonMetal.Cobalt).Hue;
            foreach (var g in CityGuard.Of(city))
            {
                foreach (var item in g.Items)
                {
                    if (item is BaseArmor)
                    {
                        Assert.Equal(cobaltHue, item.Hue);
                    }
                }
            }

            // A new holder starts from nothing.
            CityControlSystem.Capture(city, guild);
            Assert.Equal(0, CityControlSystem.GetGuardLevel(city));
            Assert.Equal(0, CityControlSystem.MagesIn(city));
        }
        finally
        {
            foreach (var g in new List<CityGuard>(CityGuard.Of(city)))
            {
                g.Delete();
            }

            leader.Delete();
            member.Delete();
        }
    }
}
