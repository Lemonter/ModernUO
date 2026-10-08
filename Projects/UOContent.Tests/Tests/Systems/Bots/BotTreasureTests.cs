using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotTreasureTests
{
    private static BotMobile NewHunter(Point3D at)
    {
        var bot = new BotMobile { Name = "Кладоискатель", Player = true, Body = 0x190, RawStr = 100 };
        bot.AddItem(new Backpack());
        bot.Skills.Wrestling.Base = 100;
        bot.Skills.Tactics.Base = 100;
        bot.Skills.Cartography.Base = 100;
        bot.Skills.Lockpicking.Base = 100;
        bot.Skills.Mining.Base = 100;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Hits = bot.HitsMax;
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    private static void Advance(int ms)
    {
        for (var elapsed = 0; elapsed < ms; elapsed += 8)
        {
            Core._tickCount += 8;
            Timer.Slice(Core.TickCount);
        }
    }

    [Fact]
    public void Hunter_KeepsOnlyMapsItCanFinish_AndSellsTheRest()
    {
        var hunter = NewHunter(new Point3D(2400, 1900, 0));
        var novice = NewHunter(new Point3D(2402, 1900, 0));
        var map = new TreasureMap(1, Map.Felucca);
        var hard = new TreasureMap(5, Map.Felucca);
        try
        {
            novice.Skills.Lockpicking.Base = 0;
            hunter.Backpack.DropItem(map);
            hunter.Backpack.DropItem(hard);
            hard.Decoder = novice; // read by someone else: worthless to this hunter

            Assert.True(BotTreasure.Keeps(hunter, map));
            Assert.False(BotTreasure.Keeps(hunter, hard));
            Assert.Same(map, BotTreasure.Usable(hunter));
            Assert.False(BotTreasure.Keeps(novice, map)); // can't open the chest

            // A thief keeps a first-level map to learn picking on its chest.
            Server.Systems.MahaonProfessions.ProfessionSystem.SetProfession(
                novice,
                Server.Systems.MahaonProfessions.MahaonProfession.Safecracker
            );
            Assert.True(BotTreasure.Keeps(novice, map));
            Assert.False(BotTreasure.Keeps(novice, new TreasureMap(2, Map.Felucca) { Decoder = novice }));

            // An unread map the bot can't use is goods; one it keeps is not.
            hunter.Brain.MarkLoot(map);
            Assert.False(BotGoods.IsForSale(hunter, map));
            var spare = new TreasureMap(2, Map.Felucca); // beyond a learning thief
            novice.Backpack.DropItem(spare);
            novice.Brain.MarkLoot(spare);
            Assert.True(BotGoods.IsForSale(novice, spare));
            Assert.Equal(300, BotGoods.BaseUnitPrice(spare));

            // A pick-hunter without picks has supply buy them first.
            Assert.True(BotTreasure.WantsLockpicks(hunter));
            Assert.Equal(0, BotGoals.TreasureHunt.Score(hunter.Brain));
        }
        finally
        {
            hunter.Delete();
            novice.Delete();
        }
    }

    [Fact]
    public void Hunter_DecodesDigsUpPicksAndLootsTheChest()
    {
        Core._tickCount = 0;
        Timer.Init(0);

        // Decoding and picking roll skill checks; the test host leaves the handlers unset.
        var locationCheck = Mobile.SkillCheckLocationHandler;
        var directLocationCheck = Mobile.SkillCheckDirectLocationHandler;
        var targetCheck = Mobile.SkillCheckTargetHandler;
        var directTargetCheck = Mobile.SkillCheckDirectTargetHandler;
        Server.Misc.SkillCheck.Initialize();

        var spot = new Point2D(2420, 1920);
        var hunter = NewHunter(new Point3D(spot.X + 1, spot.Y, Map.Felucca.GetAverageZ(spot.X + 1, spot.Y)));
        var map = new TreasureMap(1, Map.Felucca) { ChestLocation = spot };
        TreasureMapChest chest = null;
        try
        {
            hunter.Backpack.DropItem(map);
            hunter.Backpack.DropItem(new Shovel());
            hunter.Backpack.DropItem(new Lockpick(5));
            Assert.True(hunter.Hits >= hunter.HitsMax * 0.8, "hp");
            Assert.True(TreasureMap.HasDiggingTool(hunter), "tool");
            Assert.NotNull(BotTreasure.Usable(hunter));
            Assert.True(BotGoals.TreasureHunt.Score(hunter.Brain) > 0.5);

            var decode = new DecodeMapAction(map);
            for (var i = 0; i < 8 && map.Decoder == null; i++)
            {
                decode.Tick(hunter.Brain);
            }

            Assert.Same(hunter, map.Decoder);

            var dig = new DigTreasureAction(map);
            dig.Start(hunter.Brain);
            var status = dig.Tick(hunter.Brain).Status;
            Assert.Equal(BotActionStatus.Running, status);

            for (var i = 0; i < 40 && status == BotActionStatus.Running; i++)
            {
                Advance(1000);
                status = dig.Tick(hunter.Brain).Status;
            }

            Assert.Equal(BotActionStatus.Done, status);
            Assert.True(map.Completed);

            chest = BotTreasure.ChestOf(hunter, map);
            Assert.NotNull(chest);
            Assert.True(chest.Locked);

            var open = new TreasureChestAction(map);
            open.Start(hunter.Brain);
            status = BotActionStatus.Running;
            for (var i = 0; i < 60 && status == BotActionStatus.Running; i++)
            {
                status = open.Tick(hunter.Brain).Status;
                Advance(1000);
                hunter.Hits = hunter.HitsMax;
            }

            Assert.Equal(BotActionStatus.Done, status);
            Assert.False(chest.Locked);
            Assert.True(hunter.Backpack.GetAmount(typeof(Gold)) >= 1000);
        }
        finally
        {
            Mobile.SkillCheckLocationHandler = locationCheck;
            Mobile.SkillCheckDirectLocationHandler = directLocationCheck;
            Mobile.SkillCheckTargetHandler = targetCheck;
            Mobile.SkillCheckDirectTargetHandler = directTargetCheck;
            chest?.Delete();
            hunter.Delete();
        }
    }
}
