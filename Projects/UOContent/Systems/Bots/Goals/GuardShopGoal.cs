using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonGuard;

namespace Server.Systems.Bots;

public partial class BotBrain
{
    // After a purchase at the guard shop, the bot doesn't go back before this tick.
    internal long NextGuardShopTick;

    // Which mastery scroll would help, worked out from the whole skill tree, so remembered a while.
    internal int WantedMasteryScroll;
    internal long MasteryCheckedTick = long.MinValue / 2;
}

/// <summary>
/// Sergeant Guido's reward shop, as a bot uses it. A bot whose skill has run into its cap buys a
/// power scroll, one whose stats have run into theirs a stat scroll, one whose mastery has a
/// mastery scroll, when it can spare the gold. Mastery primers it leaves: they teach the SA
/// mastery abilities, which bots don't use.
/// The shop rolls the power scroll's skill among those it would raise, so the scroll may not be for
/// the skill the bot hoped for: a scroll for a skill it trains it reads, any other it sells.
/// </summary>
public static class BotScrolls
{
    // A skill the bot is actually working, not a stray point or two.
    public const double TrainedSkill = 50.0;

    private static readonly int[] PowerValues = [105, 110, 115, 120];

    /// <summary>The power scroll value that would lift a capped skill the bot works, or 0.</summary>
    public static int WantedPowerScroll(Mobile bot)
    {
        var lowestCap = double.MaxValue;
        foreach (var skill in bot.Skills)
        {
            if (skill != null && skill.Value >= TrainedSkill && skill.Value >= skill.Cap - 0.5 && skill.Cap < 120.0 &&
                skill.Cap < lowestCap)
            {
                lowestCap = skill.Cap;
            }
        }

        foreach (var value in PowerValues)
        {
            if (value > lowestCap)
            {
                return value;
            }
        }

        return 0;
    }

    private const long MasteryRecheckMs = 10 * 60_000;

    /// <summary>The mastery scroll value that would lift a mastery the bot has run into the cap
    /// of, or 0.</summary>
    public static int WantedMasteryScroll(BotBrain brain)
    {
        var now = Core.TickCount;
        if (now - brain.MasteryCheckedTick < MasteryRecheckMs)
        {
            return brain.WantedMasteryScroll;
        }

        brain.MasteryCheckedTick = now;
        brain.WantedMasteryScroll = 0;

        foreach (var value in PowerValues)
        {
            if (MahaonMasteryScroll.FindCandidates(brain.Bot, value, out var capped).Count > 0 && capped)
            {
                brain.WantedMasteryScroll = value;
                break;
            }
        }

        return brain.WantedMasteryScroll;
    }

    /// <summary>A mastery scroll the bot can read on a mastery that has hit its cap.</summary>
    public static bool Reads(Mobile bot, MahaonMasteryScroll scroll) =>
        MahaonMasteryScroll.FindCandidates(bot, scroll.Value, out var capped).Count > 0 && capped;

    /// <summary>Whether the bot's stats have reached their cap and a stat scroll would help.</summary>
    public static bool WantsStatScroll(Mobile bot) => bot.RawStatTotal >= bot.StatCap && bot.StatCap < MahaonStatScroll.MaxStatCap;

    /// <summary>A power scroll the bot can read for a skill it trains.</summary>
    public static bool Reads(Mobile bot, PowerScroll scroll) =>
        bot.Skills[scroll.Skill] is { } skill && skill.Cap < scroll.Value && skill.Value >= TrainedSkill;

    public static bool HasScrollToRead(Mobile bot)
    {
        if (bot.Backpack is not { } pack)
        {
            return false;
        }

        foreach (var scroll in pack.FindItemsByType<PowerScroll>())
        {
            if (Reads(bot, scroll))
            {
                return true;
            }
        }

        foreach (var scroll in pack.FindItemsByType<MahaonMasteryScroll>())
        {
            if (Reads(bot, scroll))
            {
                return true;
            }
        }

        return WantsStatScroll(bot) && pack.FindItemByType<MahaonStatScroll>() != null;
    }
}

public sealed class GuardShopGoal : BotGoal
{
    // A purchase leaves this much gold behind on top of the price, for supplies and repairs.
    private const int Cushion = 5000;

    public override string Name => "Лавка стражи";

    public override string[] News => ["Купил у Гвидо свиток, теперь расту дальше.", "У сержанта Гвидо свитки дорогие, но того стоят."];

    private static (int index, int price) Wanted(BotBrain brain)
    {
        var bot = brain.Bot;
        var index = -1;

        if (BotScrolls.WantedPowerScroll(bot) is > 0 and var value)
        {
            index = GuardRewardShop.IndexOf(GuardShopKind.PowerScroll, value);
        }
        else if (BotScrolls.WantsStatScroll(bot))
        {
            index = GuardRewardShop.IndexOf(GuardShopKind.StatScroll, 5);
        }
        else if (BotScrolls.WantedMasteryScroll(brain) is > 0 and var mastery)
        {
            index = GuardRewardShop.IndexOf(GuardShopKind.MasteryScroll, mastery);
        }

        return index < 0 ? (-1, 0) : (index, GuardRewardShop.PriceOf(bot, index));
    }

    private static MahaonGuardSergeant Guido(PlayerMobile bot) =>
        BotSocialRules.TownFor(bot) is { } city ? WorldCatalog.GetPlacedMobile<MahaonGuardSergeant>(city) : null;

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;

        // Scrolls already in the pack are read before anything is bought.
        if (BotScrolls.HasScrollToRead(bot))
        {
            return 0.7;
        }

        if (Core.TickCount - brain.NextGuardShopTick < 0 || Wanted(brain) is not { index: >= 0 } wanted ||
            GuardRewardShop.SpendableGold(bot) < wanted.price + Cushion || Guido(bot) == null)
        {
            return 0;
        }

        return 0.4 + BotBrain.Trait(brain.Diligence) * 0.3;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotScrolls.HasScrollToRead(bot))
        {
            return [new ReadScrollsAction()];
        }

        if (Wanted(brain) is not { index: >= 0 } wanted || Guido(bot) is not { } guido)
        {
            return null;
        }

        return [new GoToAction(guido, 2, "к сержанту Гвидо"), new GuardShopBuyAction(guido, wanted.index), new ReadScrollsAction()];
    }
}

/// <summary>Buys one line of Guido's shop, standing by him, as the gump's button does.</summary>
public sealed class GuardShopBuyAction : BotAction
{
    private const long ShopCooldownMs = 6 * 60 * 60_000L;

    private readonly Mobile _guido;
    private readonly int _index;

    public GuardShopBuyAction(Mobile guido, int index)
    {
        _guido = guido;
        _index = index;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_guido.Deleted || !bot.InRange(_guido, 3))
        {
            return BotActionResult.Failed();
        }

        bot.Direction = bot.GetDirectionTo(_guido);
        var gold = GuardRewardShop.SpendableGold(bot);
        GuardRewardShop.Buy(bot, _index);

        if (GuardRewardShop.SpendableGold(bot) >= gold)
        {
            return BotActionResult.Failed();
        }

        brain.NextGuardShopTick = Core.TickCount + ShopCooldownMs;
        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => "Покупает у сержанта Гвидо";
}

/// <summary>
/// Reads the power and stat scrolls that help the bot. A power scroll for a skill it doesn't train
/// goes to market instead.
/// </summary>
public sealed class ReadScrollsAction : BotAction
{
    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Backpack is not { } pack)
        {
            return BotActionResult.Failed();
        }

        foreach (var scroll in pack.EnumerateItemsByType<PowerScroll>())
        {
            if (BotScrolls.Reads(bot, scroll))
            {
                // The confirmation gump's OK button reads the scroll.
                scroll.Use(bot);
            }
            else if (!brain.IsLoot(scroll))
            {
                brain.MarkLoot(scroll);
            }
        }

        foreach (var scroll in pack.EnumerateItemsByType<MahaonMasteryScroll>())
        {
            if (BotScrolls.Reads(bot, scroll))
            {
                scroll.OnDoubleClick(bot);
                brain.MasteryCheckedTick = long.MinValue / 2;
            }
            else if (!brain.IsLoot(scroll))
            {
                brain.MarkLoot(scroll);
            }
        }

        if (BotScrolls.WantsStatScroll(bot) && pack.FindItemByType<MahaonStatScroll>() is { } statScroll)
        {
            statScroll.OnDoubleClick(bot);
        }

        return BotActionResult.Done(1000);
    }

    public override string Describe(BotBrain brain) => "Читает свитки";
}
