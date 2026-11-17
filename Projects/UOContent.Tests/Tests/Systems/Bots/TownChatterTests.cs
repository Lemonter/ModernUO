using System.Collections.Generic;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonCities;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class TownChatterTests
{
    private static void Advance(int ms)
    {
        for (var elapsed = 0; elapsed < ms; elapsed += 64)
        {
            Core._tickCount += 64;
            Timer.Slice(Core.TickCount);
        }
    }

    private static BotMobile NewBot(Point3D at)
    {
        var bot = new BotMobile { Name = "Тест", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void ZeroRangeSpot_TakenByAnother_BotAimsForAFreeOneNearby()
    {
        var bot = NewBot(new Point3D(1500, 1620, 0));
        var smith = NewBot(new Point3D(1502, 1620, 0));
        try
        {
            Point3D[] stands = [new(1502, 1620, 0), new(1503, 1621, 0)];
            var go = new GoToAction(Map.Felucca, stands[0], 0, "в кузницу", stands);

            go.Tick(bot.Brain);

            Assert.Equal(stands[1], go.Destination);

            // Свободное место не меняют.
            var free = new GoToAction(Map.Felucca, stands[1], 0, "в кузницу", stands);
            free.Tick(bot.Brain);
            Assert.Equal(stands[1], free.Destination);
        }
        finally
        {
            bot.Delete();
            smith.Delete();
        }
    }

    [Fact]
    public void RaidNews_OwnTownTalksOfTheOutcome_OthersHearOfItByName()
    {
        TownTalk.Clear();
        try
        {
            Assert.False(TownTalk.TryRaidTalk("Minoc", out _, out _, out _));

            TownTalk.OnRaid(TownTalk.Kind.RaidStarted, "Minoc", "Минока", "некроманта");
            TownTalk.OnRaid(TownTalk.Kind.BankBlown, "Minoc", "Минока", "некроманта");

            Assert.True(TownTalk.TryRaidTalk("Minoc", out var opener, out var reply, out var emote));
            Assert.Contains("банк", (opener + reply).ToLowerInvariant());
            Assert.NotNull(emote);

            // В чужом городе о набеге говорят, называя город.
            Assert.True(TownTalk.TryRaidTalk("Britain", out opener, out _, out _));
            Assert.Contains("Минока", opener);
        }
        finally
        {
            TownTalk.Clear();
        }
    }

    [Fact]
    public void Vendors_ChatWithANeighbour_WhoAnswers_ThenTheTownKeepsQuietAWhile()
    {
        Core._tickCount = 0;
        Timer.Init(0);

        var a = new Provisioner();
        var b = new Provisioner();
        var heard = new List<(Mobile who, string line)>();
        VendorChatter.Said = (who, line) => heard.Add((who, line));
        VendorChatter.RequireAudience = false;
        try
        {
            a.MoveToWorld(new Point3D(1450, 1650, 0), Map.Felucca);
            b.MoveToWorld(new Point3D(1452, 1650, 0), Map.Felucca);

            Assert.True(VendorChatter.TryStart(a, force: true));
            Assert.Single(heard);
            Assert.Same(a, heard[0].who);

            Advance(6000);
            Assert.Equal(2, heard.Count);
            Assert.Same(b, heard[1].who);

            Assert.False(VendorChatter.TryStart(b, force: true));
            Core._tickCount += 7 * 60_000;
            Assert.True(VendorChatter.TryStart(b, force: true));
        }
        finally
        {
            VendorChatter.Said = null;
            VendorChatter.RequireAudience = true;
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void Emote_OnAHuman_DoesNotThrow_AndPicksTheSoundBySex()
    {
        var man = NewBot(new Point3D(1500, 1630, 0));
        try
        {
            Assert.Equal(0x431, MahaonEmotes.SoundFor(man, Emote.Laugh));
            man.Female = true;
            Assert.Equal(0x321, MahaonEmotes.SoundFor(man, Emote.Laugh));
            MahaonEmotes.Do(man, Emote.Cheer);
            MahaonEmotes.Do(man, Emote.Bow);
        }
        finally
        {
            man.Delete();
        }
    }

    [Fact]
    public void CityBeacon_IsFurnishedForItsTown()
    {
        var beacon = new MahaonBotBeacon();
        try
        {
            CityFixtures.SetUpBeacon(beacon, "Britain");
            Assert.Equal("Britain", beacon.CityName);
            Assert.Equal(150, beacon.TotalLimit);

            var sum = 0;
            for (var i = 0; i < 5; i++)
            {
                var target = beacon.GetTarget((BotArchetype)i);
                Assert.True(target > 0);
                sum += target;
            }

            Assert.Equal(150, sum);
            Assert.Equal(15, beacon.ThiefTarget);

            CityFixtures.SetUpBeacon(beacon, "Ocllo");
            Assert.Equal(40, beacon.TotalLimit);
        }
        finally
        {
            beacon.Delete();
        }
    }

    [Fact]
    public void Bots_PassThroughEachOther_WithoutSpendingStamina()
    {
        var a = NewBot(new Point3D(1500, 1640, 0));
        var b = NewBot(new Point3D(1501, 1640, 0));
        try
        {
            a.RawDex = 50;
            a.Stam = 10; // при полной выносливости толкаться умеет и игрок
            Assert.True(a.CheckShove(b));
            Assert.Equal(10, a.Stam);
        }
        finally
        {
            a.Delete();
            b.Delete();
        }
    }

    [Fact]
    public void Bot_NoticesWhoStepsIntoSight_NotWhoWalksAroundAlreadySeen()
    {
        var watcher = NewBot(new Point3D(1500, 1650, 0));
        var walker = NewBot(new Point3D(1530, 1650, 0));
        try
        {
            watcher.Brain.Combat.Noticed.Clear();
            walker.Brain.Combat.Noticed.Clear();

            // Вошёл в поле зрения — заметили оба.
            BotCombat.OnSeen(watcher, walker, new Point3D(1513, 1650, 0));
            Assert.DoesNotContain(walker, watcher.Brain.Combat.Noticed);

            walker.MoveToWorld(new Point3D(1508, 1650, 0), Map.Felucca);
            BotCombat.OnSeen(watcher, walker, new Point3D(1513, 1650, 0));
            Assert.Contains(walker, watcher.Brain.Combat.Noticed);
            Assert.Contains(watcher, walker.Brain.Combat.Noticed);

            // Ходит уже на виду — не новость.
            watcher.Brain.Combat.Noticed.Clear();
            BotCombat.OnSeen(watcher, walker, new Point3D(1509, 1650, 0));
            Assert.Empty(watcher.Brain.Combat.Noticed);
        }
        finally
        {
            watcher.Delete();
            walker.Delete();
        }
    }

    [Fact]
    public void Beacon_LimitAboveItsTypeTargets_StretchesThemToTheLimit()
    {
        var beacon = new MahaonBotBeacon();
        try
        {
            beacon.TotalLimit = 600;
            beacon.SetTarget(BotArchetype.Warrior, 100);
            beacon.SetTarget(BotArchetype.Mage, 100);
            beacon.SetTarget(BotArchetype.Archer, 100);
            beacon.SetTarget(BotArchetype.Crafter, 81);
            beacon.SetTarget(BotArchetype.Trader, 100);

            Assert.Equal(600 / 481.0, beacon.Scale(), 6);

            beacon.TotalLimit = 300;
            Assert.Equal(1.0, beacon.Scale());
        }
        finally
        {
            beacon.Delete();
        }
    }

    [Fact]
    public void Bot_PassesThroughAPeacefulVendor_ButNotThroughAFightingOne()
    {
        var bot = NewBot(new Point3D(1500, 1660, 0));
        var vendor = new Provisioner();
        try
        {
            vendor.MoveToWorld(new Point3D(1501, 1660, 0), Map.Felucca);
            Assert.True(BotPassage.CanPass(bot, vendor));
            Assert.True(vendor.OnMoveOver(bot));

            vendor.Combatant = bot;
            Assert.False(BotPassage.IsPeaceful(vendor));
        }
        finally
        {
            bot.Delete();
            vendor.Delete();
        }
    }
}
