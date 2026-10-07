using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotCityControlTests
{
    private static BotMobile NewBot(Point3D at, string guildName)
    {
        var bot = new BotMobile { Name = "Боец", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Wrestling.Base = 90;
        bot.Skills.Tactics.Base = 90;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Hits = bot.HitsMax;
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null) { Caution = 20, Greed = 50 };
        BotGuilds.Join(guildName, bot);
        return bot;
    }

    private static void Cleanup(List<Mobile> mobiles, string city)
    {
        foreach (var guard in new List<CityGuard>(CityGuard.Of(city)))
        {
            guard.Delete();
        }

        foreach (var m in mobiles)
        {
            m.Delete();
        }
    }

    [Fact]
    public void GuildLeader_ClaimsAnUnheldCity_SetsTax_ThenEnemiesBesiegeAndMembersDefend()
    {
        if (!CommandSystem.Entries.ContainsKey("ClaimCity"))
        {
            CityControlSystem.Configure();
        }

        const string city = "Ocllo";
        var (center, map) = CityControlSystem.Cities[city];
        var holders = new List<Mobile>();
        var attackers = new List<Mobile>();

        try
        {
            for (var i = 0; i < 5; i++)
            {
                holders.Add(NewBot(new Point3D(center.X + i, center.Y, center.Z), "ТестХранители"));
                attackers.Add(NewBot(new Point3D(center.X + i, center.Y + 30, center.Z), "ТестОсаждающие"));
            }

            var leader = (BotMobile)holders[0];
            var guild = (Guild)leader.Guild;
            Assert.Same(leader, guild.Leader);

            Assert.Null(CityControlSystem.GetController(city));
            Assert.True(BotGoals.ClaimCity.Score(leader.Brain) > 0.4);
            Assert.Equal(0, BotGoals.ClaimCity.Score(((BotMobile)holders[1]).Brain)); // only the leader

            Assert.Equal(BotActionStatus.Done, new ClaimCityAction(city).Tick(leader.Brain).Status);
            Assert.Same(guild, CityControlSystem.GetController(city));
            Assert.Equal(15, CityControlSystem.GetTaxRate(city));
            Assert.Equal(0, BotGoals.ClaimCity.Score(leader.Brain)); // one city per guild

            // Not at war yet: the other guild leaves the city alone.
            var besieger = (BotMobile)attackers[1];
            Assert.Equal(0, BotGoals.SiegeCity.Score(besieger.Brain));

            BotGuilds.SetRelation("ТестХранители", "ТестОсаждающие", BotGuildRelation.War);
            Assert.True(BotGoals.SiegeCity.Score(besieger.Brain) > 0.3);

            // A guard in a fight calls the holders.
            CityGuard first = null;
            foreach (var g in CityGuard.Of(city))
            {
                first = g;
                break;
            }

            Assert.NotNull(first);
            Assert.Equal(6, CityGuard.Of(city).Count);
            Assert.True(CityControlSystem.IsHostileToCity(city, besieger)); // a bot-guild war counts too
            Assert.False(CityControlSystem.IsHostileToCity(city, holders[1]));
            Assert.Equal(0, BotGoals.DefendCity.Score(((BotMobile)holders[2]).Brain));
            first.Combatant = besieger;
            BotCityControl.Invalidate(city);
            Assert.Same(besieger, BotCityControl.StateOf(city).attacker);
            Assert.True(BotGoals.DefendCity.Score(((BotMobile)holders[2]).Brain) > 0.8);
            Assert.Equal(0, BotGoals.DefendCity.Score(besieger.Brain));
        }
        finally
        {
            BotGuilds.SetRelation("ТестХранители", "ТестОсаждающие", BotGuildRelation.Neutral);
            holders.AddRange(attackers);
            Cleanup(holders, city);
        }
    }
}
