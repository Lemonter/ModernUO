using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// A bot's animals, kept the way a player keeps them: it rides its horse, leads a pack animal
/// that carries the goods it gathers, and feeds them, because an unfed pet loses loyalty every
/// hour and finally walks off. Everything goes through the creatures' own handlers — mounting
/// is a double-click, feeding is food dropped on the animal.
/// </summary>
public static class BotStable
{
    // Hungry enough to feed: two hours without food. Loyalty drops a tenth an hour.
    private const int FeedBelowLoyalty = BaseCreature.MaxLoyalty - 20;
    private const long MountRetryMs = 10_000;

    // Hand goods to the pack animal once the bot itself is this full, down to the second mark.
    private const double OffloadAbove = 0.6;
    private const double OffloadDownTo = 0.35;

    public static bool IsPackAnimal(Mobile m) => m is PackHorse or PackLlama;

    /// <summary>Animals that carry or are ridden, and stay out of fights.</summary>
    public static bool IsWorkAnimal(Mobile m) => m is Horse || IsPackAnimal(m);

    private static bool IsOwnFollower(PlayerMobile bot, Mobile m) =>
        m is BaseCreature { Deleted: false, Controlled: true } c && c.ControlMaster == bot;

    /// <summary>The bot's horse standing on the ground (not the one it is riding).</summary>
    public static BaseMount UnriddenMount(PlayerMobile bot)
    {
        if (bot.AllFollowers == null)
        {
            return null;
        }

        foreach (var m in bot.AllFollowers)
        {
            if (m is BaseMount { Rider: null, IsDeadPet: false } mount && IsOwnFollower(bot, mount))
            {
                return mount;
            }
        }

        return null;
    }

    public static bool HasMount(PlayerMobile bot) => bot.Mount != null || UnriddenMount(bot) != null;

    public static BaseCreature PackAnimal(PlayerMobile bot)
    {
        if (bot.AllFollowers == null)
        {
            return null;
        }

        foreach (var m in bot.AllFollowers)
        {
            if (IsPackAnimal(m) && IsOwnFollower(bot, m) && m is BaseCreature { IsDeadPet: false } pack)
            {
                return pack;
            }
        }

        return null;
    }

    /// <summary>The pack animal's load when it is close enough to reach into.</summary>
    public static Container ReachablePack(PlayerMobile bot, int range = 2) =>
        PackAnimal(bot) is { Alive: true } animal && animal.Map == bot.Map && bot.InRange(animal, range)
            ? animal.Backpack
            : null;

    /// <summary>Every out-of-combat think: keep the animals following, fed and ridden, and the
    /// load on the pack animal rather than on the bot.</summary>
    public static void Upkeep(BotBrain brain)
    {
        var bot = brain.Bot;
        if (!bot.Alive || bot.AllFollowers is not { Count: > 0 } followers)
        {
            return;
        }

        foreach (var m in followers)
        {
            if (m is not BaseCreature { Alive: true, IsDeadPet: false } pet || !IsOwnFollower(bot, pet) ||
                pet.Map != bot.Map)
            {
                continue;
            }

            // A freshly bought animal stands where the vendor put it.
            if (pet.ControlOrder is OrderType.Stop or OrderType.None)
            {
                pet.ControlTarget = bot;
                pet.ControlOrder = OrderType.Follow;
            }

            if (pet.Loyalty < FeedBelowLoyalty && bot.InRange(pet, 2))
            {
                Feed(bot, pet);
            }
        }

        var now = Core.TickCount;
        if (!bot.Mounted && now - brain.NextMountTick >= 0 && UnriddenMount(bot) is { } mount && bot.InRange(mount, 1))
        {
            brain.NextMountTick = now + MountRetryMs;
            mount.OnDoubleClick(bot);
        }

        // On a market round the goods ride on the bot, where the vendors can see them.
        if (brain.Goal is not TradeGoal)
        {
            TryOffload(bot);
        }
    }

    /// <summary>Moves goods from the bot onto its pack animal when the bot is getting heavy.
    /// True when there was room and something moved.</summary>
    public static bool TryOffload(PlayerMobile bot)
    {
        var load = Mobile.BodyWeight + bot.TotalWeight;
        if (load < bot.MaxWeight * OffloadAbove || ReachablePack(bot) is not { } pack || bot.Backpack is not { } own)
        {
            return false;
        }

        var target = bot.MaxWeight * OffloadDownTo;
        var moved = false;

        for (var i = own.Items.Count - 1; i >= 0 && Mobile.BodyWeight + bot.TotalWeight > target; i--)
        {
            var item = own.Items[i];
            if (BotGoods.IsForSale(bot, item) && pack.TryDropItem(bot, item, false))
            {
                moved = true;
            }
        }

        return moved;
    }

    /// <summary>
    /// Brings goods back off the pack animal, as much as the bot can carry: a vendor buys only from
    /// the seller's own pack. The bot works out what fits — the most valuable goods for their
    /// weight first, splitting a stack when only part of it fits. Returns how many items moved.
    /// </summary>
    public static int Unload(PlayerMobile bot)
    {
        if (ReachablePack(bot, 3) is not { } pack || bot.Backpack is not { } own)
        {
            return 0;
        }

        var goods = new List<Item>();
        foreach (var item in pack.Items)
        {
            if (BotGoods.IsForSale(bot, item))
            {
                goods.Add(item);
            }
        }

        goods.Sort((a, b) => ValueDensity(b).CompareTo(ValueDensity(a)));

        var moved = 0;
        foreach (var item in goods)
        {
            var room = bot.MaxWeight - 10 - Mobile.BodyWeight - bot.TotalWeight;
            var unit = item.Weight;
            if (room <= 0)
            {
                break;
            }

            // TotalWeight is only what an item contains; its own weight is the pile.
            if (unit <= 0 || item.PileWeight + item.TotalWeight <= room)
            {
                own.DropItem(item);
                moved++;
                continue;
            }

            var fit = (int)(room / unit);
            if (!item.Stackable || fit <= 0)
            {
                continue;
            }

            // The remainder stays on the animal as its own stack; the part that fits comes over.
            Mobile.LiftItemDupe(item, fit);
            own.DropItem(item);
            moved++;
        }

        return moved;
    }

    private static double ValueDensity(Item item) =>
        item.Weight > 0 ? BotGoods.BaseUnitPrice(item) / item.Weight : double.MaxValue;

    private static void Feed(PlayerMobile bot, BaseCreature pet)
    {
        var pack = bot.Backpack;
        if (pack == null)
        {
            return;
        }

        foreach (var food in pack.Items)
        {
            if (food is not (Food or SheafOfHay) || !pet.CheckFoodPreference(food))
            {
                continue;
            }

            // One bite: a dropped stack would be eaten whole.
            if (food.Amount == 1)
            {
                pet.OnDragDrop(bot, food);
            }
            else if (NewBite(food) is { } bite)
            {
                if (pet.OnDragDrop(bot, bite))
                {
                    food.Consume(1);
                }
                else
                {
                    bite.Delete();
                }
            }

            return;
        }
    }

    private static Item NewBite(Item food) =>
        food switch
        {
            Apple        => new Apple(),
            SheafOfHay   => new SheafOfHay(),
            RawRibs      => new RawRibs(),
            RawFishSteak => new RawFishSteak(),
            _            => null
        };

    /// <summary>Food the bot's animals eat, to buy when it runs short, and how much.</summary>
    public static bool TryNeededFood(PlayerMobile bot, out Type type, out int amount)
    {
        type = null;
        amount = 0;

        if (bot.AllFollowers is not { Count: > 0 } followers || bot.Backpack is not { } pack)
        {
            return false;
        }

        foreach (var m in followers)
        {
            if (m is not BaseCreature pet || !IsOwnFollower(bot, pet))
            {
                continue;
            }

            var food = pet.FavoriteFood;
            var candidate =
                food.HasFlag(FoodType.FruitsAndVeggies) ? typeof(Apple) :
                food.HasFlag(FoodType.GrainsAndHay) ? typeof(SheafOfHay) :
                food.HasFlag(FoodType.Meat) ? typeof(RawRibs) :
                food.HasFlag(FoodType.Fish) ? typeof(RawFishSteak) : null;

            if (candidate != null && pack.GetAmount(candidate) < 5)
            {
                (type, amount) = (candidate, 20);
                return true;
            }
        }

        return false;
    }
}
