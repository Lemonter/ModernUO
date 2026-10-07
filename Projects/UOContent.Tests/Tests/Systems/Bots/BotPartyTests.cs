using Server.Engines.PartySystem;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonBots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotPartyTests
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
        var bot = new BotMobile { Name = "Попутчик", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Wrestling.Base = 80;
        bot.Skills.Tactics.Base = 80;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Hits = bot.HitsMax;
        BotSystem.Register(bot, bot.Location, bot.Map, null);
        bot.Brain.Sociability = 100;
        return bot;
    }

    [Fact]
    public void Bot_JoinsThePlayersParty_FollowsItsFight_LeavesWhenDismissed_AndCanBeHired()
    {
        if (PartyCommands.Handler == null)
        {
            PartyCommandHandlers.Initialize();
        }

        Core._tickCount = 0;
        Timer.Init(0);

        var player = new PlayerMobile { Name = "Игрок", Body = 0x190, Player = true };
        var friend = NewBot(new Point3D(2300, 1700, 0));
        var mercenary = NewBot(new Point3D(2302, 1700, 0));
        var foe = new Server.Mobiles.Orc();
        try
        {
            player.AddItem(new Backpack());
            player.MoveToWorld(new Point3D(2301, 1701, 0), Map.Felucca);
            foe.MoveToWorld(new Point3D(2305, 1705, 0), Map.Felucca);
            BotRelationships.OnSpokenTo(friend, player, 5);

            // Invited, a friendly bot accepts through /accept a moment later.
            Party.Invite(player, friend);
            Advance(3000);
            Assert.True(Party.Get(player)?.Contains(friend));
            Assert.True(BotGoals.FollowGroup.Score(friend.Brain) >= 2.0);

            // It fights what the player fights.
            player.Combatant = foe;
            Assert.Same(foe, friend.Brain.Group.CurrentFoe());

            // "Свободен" sends it off.
            Assert.Equal(BotIntent.Dismiss, BotConversation.Classify(BotConversation.Normalize("Свободен!")));
            BotParty.Leave(friend, "Бывай!");
            Assert.False(Party.Get(player)?.Contains(friend) == true);
            Assert.Null(friend.Brain.Group);

            // Gold below an hour's price buys nothing; an hour's price hires the bot into the party.
            var price = BotParty.HourlyPrice(mercenary);
            Assert.False(mercenary.OnDragDrop(player, new Gold(price - 1)));
            Assert.True(mercenary.OnDragDrop(player, new Gold(price * 2)));
            Assert.True(BotParty.IsHired(mercenary, player));
            Assert.True(Party.Get(player)?.Contains(mercenary));
            Assert.True(BotParty.StillServes(mercenary, mercenary.Brain.Group));

            // When the hours run out, a bot that isn't the player's friend leaves.
            Core._tickCount += 3 * 60 * 60_000L;
            Assert.False(BotParty.StillServes(mercenary, mercenary.Brain.Group));
            Assert.False(Party.Get(player)?.Contains(mercenary) == true);

            // A bot thinking well of the player leaves its own guild for the player's.
            BotGuilds.Join("ТестБродяги", friend);
            BotGuilds.Join("ТестИгроки", player);
            BotRelationships.OnSpokenTo(friend, player, 10);
            BotParty.OnGuildInvite(friend, (Guild)player.Guild, player);
            Assert.Same(player.Guild, friend.Guild);
        }
        finally
        {
            Party.Get(player)?.Disband();
            foe.Delete();
            friend.Delete();
            mercenary.Delete();
            player.Delete();
        }
    }
}
