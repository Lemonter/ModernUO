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

            // With gold in the bank and supplies at auction: wages paid, the deserter replaced for a
            // little gold, every guard handed bandages and potions, sword guards their scrolls.
            GuildBank.DepositGold(guild.Name, 100_000);
            AuctionHouseSystem.CreateListing(seller, new Bandage(200), 400, "Jhelom");
            AuctionHouseSystem.CreateListing(seller, new GreaterHealPotion { Amount = 30 }, 600, "Jhelom");
            AuctionHouseSystem.CreateListing(seller, new GreaterCurePotion { Amount = 10 }, 300, "Jhelom");
            AuctionHouseSystem.CreateListing(seller, new LightningScroll { Amount = 40 }, 400, "Jhelom");
            var before = GuildBank.GetGoldValue(guild.Name);
            CityGuardUpkeep.Run(city, guild);
            Assert.Equal(6, CityControlSystem.SwordGuardsIn(city));
            Assert.Equal(CityGuardUpkeep.ReplaceCost(0) + 5 * CityGuardUpkeep.WagePerHour(0) + 400 + 600 + 300 + 400,
                before - GuildBank.GetGoldValue(guild.Name));

            foreach (var guard in CityGuard.Of(city))
            {
                Assert.Equal(20, guard.Backpack.GetAmount(typeof(Bandage)));
                Assert.Equal(3, guard.Backpack.GetAmount(typeof(GreaterHealPotion)));
                Assert.Equal(1, guard.Backpack.GetAmount(typeof(GreaterCurePotion)));
                Assert.Equal(5, guard.Backpack.GetAmount(typeof(LightningScroll)));
            }

            // A hurt guard drinks a potion and ties a bandage.
            var drinker = new List<CityGuard>(CityGuard.Of(city))[0];
            drinker.Hits = drinker.HitsMax / 4;
            drinker.OnThink();
            Assert.Equal(2, drinker.Backpack.GetAmount(typeof(GreaterHealPotion)));
            Assert.Equal(19, drinker.Backpack.GetAmount(typeof(Bandage)));

            // A battle mage gets no scrolls and casts without them.
            Assert.True(CityControlSystem.HireMage(city, guild));
            CityGuardUpkeep.Maintain(city, guild);
            CityMageGuard mage = null;
            foreach (var guard in CityGuard.Of(city))
            {
                mage ??= guard as CityMageGuard;
            }

            Assert.NotNull(mage);
            Assert.Equal(0, mage.Backpack.GetAmount(typeof(LightningScroll)));
            Assert.True(mage.CheckSpellCast(new FireballSpell(mage)));

            // A fallen mage is replaced too, for gold alone.
            mage.Delete();
            CityGuardUpkeep.Maintain(city, guild);
            Assert.Equal(1, CityControlSystem.MagesIn(city));
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
