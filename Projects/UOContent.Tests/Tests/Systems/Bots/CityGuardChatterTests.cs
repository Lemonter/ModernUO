using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class CityGuardChatterTests
{
    private static void Advance(int ms)
    {
        for (var elapsed = 0; elapsed < ms; elapsed += 64)
        {
            Core._tickCount += 64;
            Timer.Slice(Core.TickCount);
        }
    }

    [Fact]
    public void GuardsTalkAboutWhatHappenedInTheirCity_AComradeAnswers_ThenTheyKeepQuietAWhile()
    {
        if (!CommandSystem.Entries.ContainsKey("SetCityCenter"))
        {
            CityControlSystem.Configure();
        }

        Core._tickCount = 0;
        Timer.Init(0);

        const string city = "Skara Brae";
        var (center, map) = CityControlSystem.Cities[city];
        var leader = new BotMobile { Name = "Хозяин", Player = true, Body = 0x190 };
        var heard = new List<(Mobile who, string line)>();
        CityGuardChatter.Said = (who, line) => heard.Add((who, line));
        try
        {
            leader.AddItem(new Backpack());
            leader.MoveToWorld(center, map);
            BotGuilds.Join("ТестБолтуны", leader);
            CityControlSystem.Capture(city, (Guild)leader.Guild);

            // Two guards side by side.
            var guards = new List<CityGuard>(CityGuard.Of(city));
            guards[1].MoveToWorld(new Point3D(guards[0].X + 1, guards[0].Y, guards[0].Z), guards[0].Map);

            CityGuardChatter.OnCaught(city, "Ловкач");

            var talked = false;
            for (var i = 0; i < 50 && !talked; i++)
            {
                talked = CityGuardChatter.TryStart(guards[0]);
            }

            Assert.True(talked);
            Assert.Single(heard);
            Assert.Same(guards[0], heard[0].who);

            Advance(4000);
            Assert.Equal(2, heard.Count); // a comrade answered
            Assert.NotSame(guards[0], heard[1].who);

            // The guard keeps quiet for a few minutes before talking again.
            Assert.False(CityGuardChatter.TryStart(guards[0]));
            Core._tickCount += 9 * 60_000;
            Assert.True(CityGuardChatter.TryStart(guards[0]));
        }
        finally
        {
            foreach (var g in new List<CityGuard>(CityGuard.Of(city)))
            {
                g.Delete();
            }

            CityGuardChatter.Said = null;
            leader.Delete();
        }
    }
}
