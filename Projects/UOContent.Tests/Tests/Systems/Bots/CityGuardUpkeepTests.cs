using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Spells.First;
using Server.Spells.Third;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonMetals;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class CityGuardUpkeepTests
{
    [Fact]
    public void Upkeep_PaysWages_ReplacesTheFallen_AndStocksGuardsFromTheAuction()
    {
        if (!CommandSystem.Entries.ContainsKey("SetCityCenter"))
        {
            CityControlSystem.Configure();
        }

        // Spell ids tie a cast to its scroll; the test host doesn't register the spells.
        if (Server.Spells.SpellRegistry.GetRegistryNumber(typeof(MagicArrowSpell)) < 0)
        {
            Server.Spells.Initializer.Configure();
        }

        const string city = "Jhelom";
        var (center, map) = CityControlSystem.Cities[city];
        var leader = new BotMobile { Name = "Казначей", Player = true, Body = 0x190 };
        var seller = new BotMobile { Name = "Торговка", Player = true, Body = 0x191 };
        try
        {
            leader.AddItem(new Backpack());
            seller.AddItem(new Backpack());
            seller.AddItem(new BankBox(seller));
            leader.MoveToWorld(center, map);
            seller.MoveToWorld(center, map);
            BotGuilds.Join("ТестКазармы", leader);
            var guild = (Guild)leader.Guild;
            CityControlSystem.Capture(city, guild);
            Assert.Equal(6, CityControlSystem.SwordGuardsIn(city));

            // Nothing to pay wages with: a guard walks off.
            CityGuardUpkeep.Run(city, guild);
            Assert.Equal(5, CityControlSystem.SwordGuardsIn(city));

            // With gold in the bank and potions at auction: wages paid, the deserter replaced,
            // every guard handed its potions.
            GuildBank.DepositGold(guild.Name, 100_000);
            AuctionHouseSystem.CreateListing(seller, new GreaterHealPotion { Amount = 30 }, 600, "Jhelom");
            AuctionHouseSystem.CreateListing(seller, new GreaterCurePotion { Amount = 10 }, 300, "Jhelom");
            CityGuardUpkeep.Run(city, guild);
            Assert.Equal(6, CityControlSystem.SwordGuardsIn(city));
            foreach (var guard in CityGuard.Of(city))
            {
                Assert.Equal(3, guard.Backpack.GetAmount(typeof(GreaterHealPotion)));
                Assert.Equal(1, guard.Backpack.GetAmount(typeof(GreaterCurePotion)));
            }

            // A hurt guard drinks one.
            var drinker = new List<CityGuard>(CityGuard.Of(city))[0];
            drinker.Hits = drinker.HitsMax / 4;
            drinker.OnThink();
            Assert.Equal(2, drinker.Backpack.GetAmount(typeof(GreaterHealPotion)));

            // A battle mage gets scrolls and burns one per spell; without the scroll it can't cast.
            Assert.True(CityControlSystem.HireMage(city, guild));
            AuctionHouseSystem.CreateListing(seller, new MagicArrowScroll { Amount = 20 }, 200, "Jhelom");
            CityGuardUpkeep.Run(city, guild);
            CityMageGuard mage = null;
            foreach (var guard in CityGuard.Of(city))
            {
                mage ??= guard as CityMageGuard;
            }

            Assert.NotNull(mage);
            Assert.Equal(5, mage.Backpack.GetAmount(typeof(MagicArrowScroll)));
            Assert.True(mage.CheckSpellCast(new MagicArrowSpell(mage)));
            Assert.Equal(4, mage.Backpack.GetAmount(typeof(MagicArrowScroll)));
            Assert.False(mage.CheckSpellCast(new FireballSpell(mage)));

            // A fallen mage is replaced too.
            mage.Delete();
            CityGuardUpkeep.Run(city, guild);
            Assert.Equal(1, CityControlSystem.MagesIn(city));

            // At a metal level the replacement needs ingots, bought at the auction when the bank has none.
            GuildBank.DepositGold(guild.Name, 50_000);
            leader.Backpack.DropItem(new MahaonIngot(MahaonMetal.Iron, 1000));
            GuildBank.Deposit(guild.Name, leader.Backpack.FindItemByType<MahaonIngot>());
            Assert.True(CityControlSystem.UpgradeGuards(city, guild));
            AuctionHouseSystem.CreateListing(seller, new MahaonIngot(MahaonMetal.Iron, 60), 300, "Jhelom");
            new List<CityGuard>(CityGuard.Of(city))[0].Delete();
            CityGuardUpkeep.Run(city, guild);
            Assert.Equal(6, CityControlSystem.SwordGuardsIn(city));
            Assert.True(seller.BankBox.GetAmount(typeof(Gold)) > 0);
        }
        finally
        {
            foreach (var g in new List<CityGuard>(CityGuard.Of(city)))
            {
                g.Delete();
            }

            leader.Delete();
            seller.Delete();
        }
    }
}
