using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;
using Server.Spells.Necromancy;
using Server.Spells.Chivalry;
using Server.Spells.Bushido;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonCombat;
using Server.Systems.MahaonMining;
using Server.Systems.MahaonProfessions;
using Server.Systems.MahaonRecipes;
using Server.Systems.MahaonRaids;
using Server.Targeting;

namespace Server.Systems.MahaonBots;

/// <summary>Crafting, skill training, and trading/economy — spending gold, buying blueprints/materials, selling loot, auction listing.</summary>
public partial class BotController
{

    private static void TrySpendGold(PlayerMobile bot)
    {
        EnforcePocketCap(bot);
        RestockGatherTools(bot);

        if (Utility.RandomDouble() > 0.3)
        {
            return; // don't roll this every single idle tick
        }

        var backpack = bot.Backpack;
        var bank = bot.BankBox;
        if (backpack == null || bank == null)
        {
            return;
        }

        var available = Banker.GetBalance(bot);
        if (available <= 0)
        {
            return;
        }

        if (GetArchetype(bot) == BotArchetype.Crafter)
        {
            TryBuyBlueprint(bot, bank, available);
        }
        else
        {
            TryTrainSkill(bot, bank, available);
        }
    }

    /// <summary>
    ///     Выдаёт ремесленнику весь набор чертежей на оружие и броню сразу при создании.
    ///
    ///     Задумано было именно так — см. комментарий в DoCrafting про «craft bots know
    ///     every blueprint». На деле бот начинал не зная ни одного и должен был копить на
    ///     них в банке, а пока копил — каждый ход жаловался вслух, что денег на чертёж не
    ///     хватает, и брался крафтить наугад. Теперь чертежи есть с самого начала, а
    ///     TryBuyBlueprint для него просто нечего покупать и он молчит.
    /// </summary>
    public static void GrantStartingRecipes(PlayerMobile bot)
    {
        if (bot == null)
        {
            return;
        }

        foreach (var recipe in Recipe.Recipes.Values)
        {
            if (IsWeaponOrArmorRecipe(recipe.CraftItem) && !bot.HasRecipe(recipe))
            {
                bot.AcquireRecipe(recipe);
            }
        }
    }

    /// <summary>Restricts crafters to armor and weapons of any kind (plate, leather, bows,
    /// swords, axes, whatever their craft skill actually makes) — previously bots learned
    /// and made whatever recipe happened to be cheapest/first, furniture and deco included.</summary>
    private static bool IsWeaponOrArmorRecipe(CraftItem craftItem) =>
        craftItem?.ItemType != null &&
        (typeof(BaseWeapon).IsAssignableFrom(craftItem.ItemType) || typeof(BaseArmor).IsAssignableFrom(craftItem.ItemType));

    private static void TryBuyBlueprint(PlayerMobile bot, Container backpack, long availableCopper)
    {
        Recipe cheapest = null;
        var cheapestPrice = long.MaxValue;

        foreach (var recipe in Recipe.Recipes.Values)
        {
            if (bot.HasRecipe(recipe) || !IsWeaponOrArmorRecipe(recipe.CraftItem))
            {
                continue;
            }

            var price = MahaonBlueprint.GetPrice(recipe);
            if (price < cheapestPrice)
            {
                cheapestPrice = price;
                cheapest = recipe;
            }
        }

        if (cheapest == null)
        {
            return; // already knows every recipe there is
        }

        var cost = cheapestPrice;
        if (cost > availableCopper)
        {
            // Раньше это выкрикивалось каждый ход, пока бот копил — со стороны выглядело
            // как непрерывное нытьё про чертежи. Копит молча.
            return;
        }

        if (!Banker.Withdraw(bot, (int)cost))
        {
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Не смог снять деньги на чертёж — что-то не так с монетами в банке.");
            return;
        }

        bot.AcquireRecipe(cheapest);
        bot.PublicOverheadMessage(MessageType.Regular, 0x59, false, $"Купил чертёж: {RecipeNameHelper.GetName(cheapest)}.");
    }

    private static readonly SkillName[] WarriorTrainSkills = { SkillName.Swords, SkillName.Tactics, SkillName.Anatomy };
    private static readonly SkillName[] MageTrainSkills = { SkillName.Magery, SkillName.EvalInt, SkillName.Meditation };
    private static readonly SkillName[] ArcherTrainSkills = { SkillName.Archery, SkillName.Tactics };

    private const long TrainCostPerPoint = 50;
    private const double TrainPointsMin = 1.0;
    private const double TrainPointsMax = 3.0;

    private static void TryTrainSkill(PlayerMobile bot, Container backpack, long availableCopper)
    {
        var profession = ProfessionSystem.GetProfession(bot);

        SkillName skillName;
        var trainingSecondary = false;

        if (profession != null && Utility.RandomDouble() < BotTuning.CraftOwnProfessionChance)
        {
            var info = ProfessionData.All[profession.Value];
            var primarySkills = ProfessionData.CategorySkills[info.Category];
            skillName = primarySkills[Utility.Random(primarySkills.Length)];
        }
        else
        {
            var skills = GetArchetype(bot) switch
            {
                BotArchetype.Warrior => WarriorTrainSkills,
                BotArchetype.Mage    => MageTrainSkills,
                BotArchetype.Archer  => ArcherTrainSkills,
                _                    => WarriorTrainSkills
            };

            skillName = skills.RandomElement();
            trainingSecondary = true;
        }

        var skill = bot.Skills[skillName];
        if (skill.Base >= skill.Cap)
        {
            return; // already at this skill's cap (100 normal, 120 for the primary class kit)
        }

        var points = TrainPointsMin + (TrainPointsMax - TrainPointsMin) * Utility.RandomDouble();
        var cost = (long)(points * TrainCostPerPoint);

        if (cost > availableCopper || !Banker.Withdraw(bot, (int)cost))
        {
            return;
        }

        skill.Base = System.Math.Min(skill.Cap, skill.Base + points);

        var verb = trainingSecondary ? "Позанимаюсь ещё" : "Потренирую";
        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"{verb} немного скилл «{skill.Name}».");
    }

    // Generous on purpose — gold is scarce for training skills, but stats are meant to grow
    // from just doing things, same as real UO's classic stat-gain-on-use.
    private const double StatGainChance = 0.08;

    /// <summary>Rolls a chance to tick the given stat up by 1 (toward this bot's profession
    /// cap), same as real UO's stat-gain-on-use — call this from real action points
    /// (landing a hit, taking damage, successful cast, successful harvest), not on a timer.</summary>
    private static void TryGainStat(Mobile bot, StatType stat)
    {
        if (Utility.RandomDouble() >= StatGainChance)
        {
            return;
        }

        var cap = ProfessionSystem.GetStatCap(bot, stat);

        var current = stat switch
        {
            StatType.Str => bot.RawStr,
            StatType.Dex => bot.RawDex,
            _            => bot.RawInt
        };

        if (current >= cap)
        {
            return;
        }

        switch (stat)
        {
            case StatType.Str:
                bot.RawStr = current + 1;
                break;
            case StatType.Dex:
                bot.RawDex = current + 1;
                break;
            default:
                bot.RawInt = current + 1;
                break;
        }
    }

    private static void DoCrafting(PlayerMobile bot, BotProfile profile)
    {
        if (profile.CyclesRemaining-- <= 0)
        {
            profile.Activity = BotActivity.TravelingToMarket;
            return;
        }

        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return;
        }

        // Raw materials still need turning into ingots/boards first — that part stays a
        // simplified stand-in (no real forge-fire minigame), but still gated by a real
        // skill check.
        var ore = backpack.FindItemByType<IronOre>();
        var logs = backpack.FindItemByType<Log>();

        if (ore != null)
        {
            if (bot.CheckSkill(SkillName.Blacksmith, 0.0, 100.0))
            {
                ore.Consume(Math.Min(ore.Amount, 5));
                backpack.DropItem(new IronIngot(1));
            }

            return;
        }

        if (logs != null)
        {
            if (bot.CheckSkill(SkillName.Carpentry, 0.0, 100.0))
            {
                logs.Consume(Math.Min(logs.Amount, 5));
                backpack.DropItem(new Board(1));
            }

            return;
        }

        // No raw materials left to process — try an actual craft attempt through the real
        // DefCraftSystem, using whatever recipe this bot has actually learned (the same
        // MahaonBlueprint system players use). No shortcuts here: real tool, real resource
        // consumption, real success chance off the real skill.
        TryRealCraft(bot, backpack);
    }

    private static void TryRealCraft(PlayerMobile bot, Container backpack)
    {
        var tool = backpack.FindItemByType<BaseTool>();
        if (tool == null)
        {
            return;
        }

        var knownCount = 0;

        foreach (var recipe in Recipe.Recipes.Values)
        {
            if (recipe.CraftSystem != tool.CraftSystem || !IsWeaponOrArmorRecipe(recipe.CraftItem))
            {
                continue;
            }

            knownCount++;

            var craftItem = recipe.CraftItem;
            if (craftItem?.Resources == null || craftItem.Resources.Count == 0)
            {
                continue;
            }

            // Only the simple single-resource-type recipes are worth a bot's time here —
            // enough to cover most early armor/weapon/tool recipes without needing a full
            // multi-resource-slot resolver.
            var resType = craftItem.Resources[0].ItemType;
            var needed = craftItem.Resources[0].Amount;
            var have = backpack.GetAmount(resType);

            if (have < needed)
            {
                // Deliberate cheat, per the request — craft bots know every blueprint and
                // pull materials from thin air instead of actually gathering them.
                try
                {
                    var conjured = (Item)Activator.CreateInstance(resType, needed - have);
                    backpack.DropItem(conjured);
                }
                catch
                {
                    continue; // this particular resource type doesn't support this — skip
                }
            }

            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Попробую сделать: {RecipeNameHelper.GetName(recipe)}.");
            craftItem.Craft(bot, recipe.CraftSystem, resType, tool);
            return;
        }

        // Diagnostic — nothing got crafted, so say exactly why so it's obvious from the
        // journal instead of a silent no-op.
        if (knownCount == 0)
        {
            // Молча: инструмент не под ту специальность, это не повод кричать.
            return;
        }
        else
        {
            // Тоже молча — материалы бот и так себе наколдует на следующем ходу.
        }
    }

    private const long TraderBuyBudget = 500;
    private const double TraderMarkup = 1.4; // resell at +40%

    private static void DoTraderTrade(PlayerMobile bot, string city)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return;
        }

        if (CityMarkers.TryGetMarker(city, "auctionstone", out var stoneLoc, out var stoneMap) && bot.Map == stoneMap)
        {
            bot.Location = stoneLoc; // already just teleported into the city via CityTravel — this just fine-tunes to the exact stone
        }

        // Sell anything already being carried (bought in a previous city) at a markup.
        List<Item> carried = null;
        foreach (var item in backpack.Items)
        {
            if (item is Gold)
            {
                continue;
            }

            (carried ??= new List<Item>()).Add(item);
        }

        if (carried != null)
        {
            foreach (var item in carried)
            {
                var value = EstimateValue(item);
                if (value > 0)
                {
                    AuctionHouseSystem.CreateListing(bot, item, (long)(value * TraderMarkup), city);
                }
            }
        }

        // Pick up the cheapest listing here to carry to the next city.
        AuctionListing cheapest = null;

        foreach (var listing in AuctionHouseSystem.ActiveListings(city))
        {
            if (listing.Seller == bot)
            {
                continue;
            }

            if (cheapest == null || listing.Price < cheapest.Price)
            {
                cheapest = listing;
            }
        }

        if (cheapest == null || cheapest.Price > TraderBuyBudget)
        {
            return;
        }

        AuctionHouseSystem.TryBuy(bot, cheapest.Id);
    }

    private static long EstimateValue(Item item) => item switch
    {
        IronIngot ingot => ingot.Amount * 8L,
        Board board     => board.Amount * 8L,
        IronOre ore     => ore.Amount * 3L,
        Log log         => log.Amount * 3L,
        _               => 0L
    };

    private static void DoTravelToMarket(PlayerMobile bot, BotProfile profile)
    {
        var (marketLoc, marketMap) = NearestMarket(bot, profile);

        if (StepTowardPath(bot, profile, marketLoc) && bot.Map == marketMap)
        {
            profile.Activity = BotActivity.Selling;
        }
    }

    private static void DoSelling(PlayerMobile bot, BotProfile profile)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            profile.Activity = BotActivity.Idle;
            return;
        }

        var city = profile.CurrentCity ?? profile.HomeCity ?? "Britain";

        ListForAuction<IronIngot>(bot, backpack, 8, city);
        ListForAuction<Board>(bot, backpack, 8, city);
        ListForAuction<IronOre>(bot, backpack, 3, city);
        ListForAuction<Log>(bot, backpack, 3, city);
        ListForAuction<Fish>(bot, backpack, 5, city);
        ListSpareLootForAuction(bot, backpack, city);
        TryBuyNeededMaterials(bot, city);

        profile.Activity = BotActivity.Idle;
    }

    /// <summary>
    ///     Simplified "is this cheaper than the vendor" check — reagents run roughly 4-6gp
    ///     at an NPC in practice, so anything listed under that on the auction is a real
    ///     bargain worth grabbing instead. Not a live price feed against the actual vendor
    ///     stock, just a reasonable stand-in threshold.
    /// </summary>
    private const long ReagentVendorPriceEstimate = 5;

    private static void TryBuyNeededMaterials(PlayerMobile bot, string city)
    {
        var profession = ProfessionSystem.GetProfession(bot);
        if (profession == null)
        {
            return;
        }

        var info = ProfessionData.All[profession.Value];
        var needsReagents = info.Category == ProfessionCategory.Magic || info.SecondaryCategory == ProfessionCategory.Magic;

        if (!needsReagents)
        {
            return;
        }

        foreach (var listing in AuctionHouseSystem.ActiveListings(city))
        {
            if (listing.Seller == bot)
            {
                continue;
            }

            var isReagent = listing.Item is Garlic or Ginseng or MandrakeRoot or SpidersSilk or BlackPearl or SulfurousAsh or Nightshade;
            if (!isReagent || listing.Price > ReagentVendorPriceEstimate * System.Math.Max(1, listing.Item.Amount))
            {
                continue;
            }

            AuctionHouseSystem.TryBuy(bot, listing.Id);
        }
    }

    /// <summary>
    ///     Corpse loot that isn't this bot's own gear (armor/weapons that weren't upgrades,
    ///     reagents a non-caster doesn't use) shouldn't just sit in the pack — a real player
    ///     would sell it. Reagents fetch a small flat price; armor/weapons get valued by a
    ///     rough tier estimate rather than a real appraisal.
    /// </summary>
    private static void ListSpareLootForAuction(PlayerMobile bot, Container backpack, string city)
    {
        List<(Item item, long price)> spare = null;

        foreach (var item in backpack.Items)
        {
            long price = item switch
            {
                Garlic or Ginseng or MandrakeRoot or SpidersSilk or BlackPearl or SulfurousAsh or Nightshade
                    => 4L * item.Amount,
                BaseArmor armor when bot.FindItemOnLayer(armor.Layer) != armor => 60L,
                BaseWeapon weapon when bot.FindItemOnLayer(weapon.Layer) != weapon => 80L,
                _ => 0L
            };

            if (price > 0)
            {
                (spare ??= new List<(Item, long)>()).Add((item, price));
            }
        }

        if (spare == null)
        {
            return;
        }

        foreach (var (item, price) in spare)
        {
            AuctionHouseSystem.CreateListing(bot, item, price, city);
        }
    }

    private static void ListForAuction<T>(PlayerMobile bot, Container backpack, int copperPerUnit, string city) where T : Item
    {
        var item = backpack.FindItemByType<T>();
        if (item == null)
        {
            return;
        }

        var price = (long)item.Amount * copperPerUnit;
        if (price <= 0)
        {
            return;
        }

        AuctionHouseSystem.CreateListing(bot, item, price, city);
    }

    private static (Point3D loc, Map map) NearestMarket(PlayerMobile bot, BotProfile profile)
    {
        var city = profile.CurrentCity ?? profile.HomeCity;

        if (city != null)
        {
            if (CityMarkers.TryGetMarker(city, "auctionstone", out var loc, out var map))
            {
                return (loc, map);
            }

            if (CityControlSystem.Cities.TryGetValue(city, out var info))
            {
                return (info.spawn, info.map);
            }
        }

        return (bot.Location, bot.Map); // no known city — just "sell" on the spot
    }
}
