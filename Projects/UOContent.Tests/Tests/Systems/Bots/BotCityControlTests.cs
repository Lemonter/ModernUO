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

    private static void Advance(int ms)
    {
        for (var elapsed = 0; elapsed < ms; elapsed += 64)
        {
            Core._tickCount += 64;
            Timer.Slice(Core.TickCount);
        }
    }

    [Fact]
    public void Claim_IsHeldAtTheBannerForTenMinutes_ThenGuardsAndHoldersDefendIt()
    {
        if (!CommandSystem.Entries.ContainsKey("SetCityCenter"))
        {
            CityControlSystem.Configure();
        }

        Core._tickCount = 0;
        Timer.Init(0);

        const string city = "Ocllo";
        var (center, map) = CityControlSystem.Cities[city];
        var banner = new MahaonCityClaimPoint(city);
        var holders = new List<Mobile>();
        var attackers = new List<Mobile>();

        try
        {
            banner.MoveToWorld(center, map);
            Assert.Same(banner, MahaonCityClaimPoint.Of(city));

            for (var i = 0; i < 5; i++)
            {
                holders.Add(NewBot(new Point3D(center.X + i, center.Y + 1, center.Z), "ТестХранители"));
                attackers.Add(NewBot(new Point3D(center.X + i, center.Y + 60, center.Z), "ТестОсаждающие"));
            }

            var leader = (BotMobile)holders[0];
            var guild = (Guild)leader.Guild;

            // An unheld city: the leader claims it at the banner and its guild holds it ten minutes.
            Assert.True(BotGoals.ClaimCity.Score(leader.Brain) > 0.4);
            Assert.Equal(0, BotGoals.ClaimCity.Score(((BotMobile)holders[1]).Brain)); // only the leader

            Assert.Equal(BotActionStatus.Done, new ClaimCityAction(city).Tick(leader.Brain).Status);
            Assert.True(banner.Contested);
            Assert.Same(guild, banner.Contender);
            Assert.True(BotGoals.SiegeCity.Score(((BotMobile)holders[1]).Brain) > 0.8);
            Assert.Null(CityControlSystem.GetController(city));

            Advance(9 * 60_000);
            Assert.Null(CityControlSystem.GetController(city)); // not yet

            Advance(61_000);
            Assert.False(banner.Contested);
            Assert.Same(guild, CityControlSystem.GetController(city));
            Assert.Equal(6, CityGuard.Of(city).Count);

            // The holding action ends with the claim won, and the leader sets the tax.
            Assert.Equal(BotActionStatus.Done, new BannerFightAction(city, false).Tick(leader.Brain).Status);
            Assert.Equal(15, CityControlSystem.GetTaxRate(city));
            Assert.Equal(0, BotGoals.ClaimCity.Score(leader.Brain)); // one city per guild

            // A guild at war claims it back: guards rally to the banner, holders come to defend.
            BotGuilds.SetRelation("ТестХранители", "ТестОсаждающие", BotGuildRelation.War);
            var rival = (BotMobile)attackers[0];
            Assert.True(CityControlSystem.IsHostileToCity(city, rival));
            Assert.False(CityControlSystem.IsHostileToCity(city, holders[1]));

            rival.MoveToWorld(new Point3D(center.X - 2, center.Y, center.Z), map);
            Assert.True(BotGoals.ClaimCity.Score(rival.Brain) > 0.4);
            Assert.Equal(BotActionStatus.Done, new ClaimCityAction(city).Tick(rival.Brain).Status);
            Assert.Same(rival.Guild, banner.Contender);

            foreach (var guard in CityGuard.Of(city))
            {
                Assert.Equal(banner.Location, guard.Home);
            }

            Assert.True(BotGoals.DefendCity.Score(((BotMobile)holders[2]).Brain) > 0.8);

            // Every claimant gone from the banner: the claim fails and the guards go back to their posts.
            rival.MoveToWorld(new Point3D(center.X, center.Y + 60, center.Z), map);
            Advance(6000);
            Assert.False(banner.Contested);
            Assert.Same(guild, CityControlSystem.GetController(city));
            foreach (var guard in CityGuard.Of(city))
            {
                Assert.Equal(guard.Post, guard.Home);
            }
        }
        finally
        {
            BotGuilds.SetRelation("ТестХранители", "ТестОсаждающие", BotGuildRelation.Neutral);
            banner.Delete();
            foreach (var guard in new List<CityGuard>(CityGuard.Of(city)))
            {
                guard.Delete();
            }

            foreach (var m in holders)
            {
                m.Delete();
            }

            foreach (var m in attackers)
            {
                m.Delete();
            }
        }
    }
}
