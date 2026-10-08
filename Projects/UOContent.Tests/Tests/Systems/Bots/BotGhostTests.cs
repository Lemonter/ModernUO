using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotGhostTests
{
    private static BotMobile NewBot(Point3D at)
    {
        var bot = new BotMobile { Name = "Призрак", Player = true, Body = 0x190 };
        bot.MoveToWorld(at, Map.Felucca);
        return bot;
    }

    [Fact]
    public void Healers_RefuseOutlaws_EvilHealersTakeMurderers()
    {
        var bot = NewBot(new Point3D(1700, 1700, 0));
        var healer = new Healer();
        var evil = new EvilHealer();
        try
        {
            Assert.True(BotGhost.Accepts(healer, bot));

            bot.Kills = 5;
            Assert.True(bot.Murderer);
            Assert.False(BotGhost.Accepts(healer, bot));
            Assert.True(BotGhost.Accepts(evil, bot));
        }
        finally
        {
            bot.Delete();
            healer.Delete();
            evil.Delete();
        }
    }

    [Fact]
    public void Murderer_HeadsForAnkh_NotForTheHealerBesideIt()
    {
        var bot = NewBot(new Point3D(1700, 1700, 0));
        var healer = new Healer();
        var ankh = new AnkhWest();
        try
        {
            healer.MoveToWorld(new Point3D(1703, 1700, 0), Map.Felucca);
            ankh.MoveToWorld(new Point3D(1720, 1700, 0), Map.Felucca);
            ShrineAtlas.AddSpot(Map.Felucca, ankh.Location);

            Assert.Same(healer, BotGhost.ChooseTarget(bot));

            bot.Kills = 5;
            Assert.Same(ankh, BotGhost.ChooseTarget(bot));
        }
        finally
        {
            ShrineAtlas.Clear();
            bot.Delete();
            healer.Delete();
            ankh.Delete();
        }
    }
}
