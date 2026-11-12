using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotPkRulesTests
{
    private static BotMobile NewBot(string name, Point3D at)
    {
        var bot = new BotMobile { Name = name, Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void PkBot_KillingABot_IsReported_AndStaysBlueUntilFiveKills()
    {
        var pk = NewBot("Душегуб", new Point3D(1600, 1700, 0));
        pk.IsPk = true;

        // Тестовый хост не поднимает систему VvV, а её обработчик смерти ждёт экземпляр.
        var vvv = Server.Engines.VvV.ViceVsVirtueSystem.Enabled;
        Server.Engines.VvV.ViceVsVirtueSystem.Enabled = false;

        // Нотория решает, преступно ли нападение, а значит — можно ли сообщить об убийце.
        var notoriety = Notoriety.Handler;
        var allowHarmful = Mobile.AllowHarmfulHandler;
        var allowBeneficial = Mobile.AllowBeneficialHandler;
        Server.Misc.NotorietyHandlers.Initialize();

        try
        {
            // Роль ПК сама по себе вне закона не делает.
            Assert.False(BotSocialRules.IsOutlaw(pk));

            for (var i = 1; i <= 5; i++)
            {
                var victim = NewBot($"Жертва{i}", new Point3D(1601, 1700, 0));
                try
                {
                    pk.DoHarmful(victim);
                    victim.Kill();

                    Assert.Equal(i, pk.Kills);
                }
                finally
                {
                    victim.Delete();
                }

                pk.Criminal = false;
            }

            Assert.True(pk.Murderer);
            Assert.True(BotSocialRules.IsOutlaw(pk));
        }
        finally
        {
            Server.Engines.VvV.ViceVsVirtueSystem.Enabled = vvv;
            Notoriety.Handler = notoriety;
            Mobile.AllowHarmfulHandler = allowHarmful;
            Mobile.AllowBeneficialHandler = allowBeneficial;
            pk.Delete();
        }
    }
}
