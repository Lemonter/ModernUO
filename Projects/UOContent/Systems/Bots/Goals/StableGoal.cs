using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Buys a horse to ride and, for a bot that hauls goods, a pack animal — from a town's animal
/// trainer at the shop price, once it can spare the money. The animals are then kept by
/// <see cref="BotStable.Upkeep"/>.
/// </summary>
public sealed class StableGoal : BotGoal
{
    public override string Name => "Конюшня";

    public override bool IsUpkeep => true;

    public override string[] News => ["Обзавёлся скотиной, теперь не пешком.", "Купил лошадку, красавица."];

    // Money the bot keeps after the purchase, so a horse doesn't leave it unable to buy tools.
    private const int Reserve = 600;
    private const int HorsePrice = 550;
    private const int PackAnimalPrice = 631;
    private const int MinGatherSkill = 40;

    private static readonly Type[] _packAnimals = [typeof(PackLlama), typeof(PackHorse)];

    /// <summary>The bot fills packs for a living: a gatherer or a crafter.</summary>
    internal static bool Hauls(Mobile bot) =>
        bot.Skills.Mining.Value >= MinGatherSkill || bot.Skills.Lumberjacking.Value >= MinGatherSkill ||
        bot.Skills.Fishing.Value >= MinGatherSkill || BotCrafting.IsAnyCrafter(bot);

    private static long Funds(Mobile bot) => (bot.Backpack?.GetAmount(typeof(Items.Gold)) ?? 0) + Banker.GetBalance(bot);

    private static bool HasSlot(Mobile bot) => bot.Followers + 1 <= bot.FollowersMax;

    private static Type[] Wanted(PlayerMobile bot)
    {
        if (!HasSlot(bot))
        {
            return null;
        }

        var funds = Funds(bot);

        if (!BotStable.HasMount(bot) && funds >= HorsePrice + Reserve)
        {
            return [typeof(Horse)];
        }

        if (BotStable.PackAnimal(bot) == null && Hauls(bot) && funds >= PackAnimalPrice + Reserve)
        {
            return _packAnimals;
        }

        return null;
    }

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (Wanted(bot) is not { } wanted)
        {
            return 0;
        }

        return wanted[0] == typeof(Horse)
            ? 0.45 + BotBrain.Trait(brain.Wanderlust) * 0.25
            : 0.4 + BotBrain.Trait(brain.Diligence) * 0.25;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var city = BotSocialRules.TownFor(bot);
        if (city == null || Wanted(bot) is not { } wanted)
        {
            return null;
        }

        BaseVendor seller = null;
        Type type = null;
        var price = 0;

        foreach (var vendor in WorldCatalog.GetVendors(city))
        {
            foreach (var candidate in wanted)
            {
                if (BuyFromVendorAction.FindStock(vendor, candidate) is { } stock && stock.Type == candidate &&
                    (seller == null || stock.Price < price))
                {
                    (seller, type, price) = (vendor, candidate, stock.Price);
                }
            }
        }

        if (seller == null)
        {
            return null;
        }

        var steps = new List<BotAction>();
        var purse = bot.Backpack?.GetAmount(typeof(Items.Gold)) ?? 0;

        if (purse < price)
        {
            var banker = WorldCatalog.GetBanker(city);
            if (banker == null || Banker.GetBalance(bot) < price - purse)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, price - purse));
        }

        steps.Add(new GoToAction(seller, 2, $"к торговцу {seller.Name}"));
        steps.Add(new BuyFromVendorAction(seller, type, 1));
        return steps;
    }
}
