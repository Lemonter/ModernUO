using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonQuests;

namespace Server.Systems.Bots;

public partial class BotBrain
{
    // What the bot last read off the town boards; boards are the only way it learns of them.
    internal Mobile KnownBounty;
    internal int SeenOrderStamp = -1;
}

/// <summary>
/// The crafting guild's order board: a bot that has read the current order and carries enough
/// of what it wants takes it to the board. A bot that hasn't read it yet goes to look when it
/// carries a fair stack of something the guild tends to ask for.
/// </summary>
public sealed class GuildOrderGoal : BotGoal
{
    // Below this a stack isn't worth the walk to a board that may want something else.
    private const int LookStack = 20;

    // What the bot would deliver: goods it gathers or crafts for sale, never its own consumables.
    private static readonly Type[] Deliverable = [typeof(Board), typeof(Leather), typeof(MahaonIngot), typeof(Arrow)];

    public override string Name => "Заказ гильдии";

    public override string[] News => ["Сдал заказ в гильдию ремесленников, платят исправно.", "Гильдия опять скупает товар, успевай носить."];

    private static bool Delivers(PlayerMobile bot, Type type) =>
        type != null && Array.IndexOf(Deliverable, type) >= 0 &&
        // An archer's arrows are ammunition; only a bowyer's are goods.
        (type != typeof(Arrow) || bot.Skills.Fletching.Value >= BotCrafting.CrafterSkill && bot.Weapon is not BaseRanged);

    private static MahaonCraftingOrderBoard Board(PlayerMobile bot) =>
        BotSocialRules.TownFor(bot) is { } city ? WorldCatalog.GetPlaced<MahaonCraftingOrderBoard>(city) : null;

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;
        if (pack == null || Board(bot) == null)
        {
            return 0;
        }

        if (brain.SeenOrderStamp == CraftingGuildSystem.OrderStamp)
        {
            var type = CraftingGuildSystem.CurrentItemType;
            return Delivers(bot, type) && CraftingGuildSystem.CountIn(pack) >= CraftingGuildSystem.CurrentAmount
                ? 0.6 + BotBrain.Trait(brain.Greed) * 0.3
                : 0;
        }

        foreach (var type in Deliverable)
        {
            if (Delivers(bot, type) && pack.GetAmount(type) >= LookStack)
            {
                return 0.3 + BotBrain.Trait(brain.Greed) * 0.2;
            }
        }

        return 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var board = Board(brain.Bot);
        return board == null ? null : [new GoToAction(board.Map, board.Location, 2, "к доске заказов"), new UseBoardAction(board)];
    }
}

/// <summary>Double-clicks a quest board as a player does, and remembers what it said.</summary>
public sealed class UseBoardAction : BotAction
{
    private readonly Item _board;

    public UseBoardAction(Item board) => _board = board;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_board.Deleted || !bot.InRange(_board.GetWorldLocation(), 3))
        {
            return BotActionResult.Failed();
        }

        bot.Direction = bot.GetDirectionTo(_board);

        if (_board is MahaonCraftingOrderBoard)
        {
            _board.OnDoubleClick(bot);

            // A fulfilled order rolls a new one, which the bot has just seen on the board too.
            brain.SeenOrderStamp = CraftingGuildSystem.OrderStamp;
        }
        else if (_board is MahaonBountyBoard)
        {
            _board.OnDoubleClick(bot);
            brain.KnownBounty = BountyHunterSystem.CurrentTarget;
        }

        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => "Читает доску";
}

/// <summary>
/// The bounty board names one outlaw and a reward. A strong, law-abiding fighter who has read
/// the board hunts the outlaw down when it is in the same world and not out of its league; one
/// who hasn't goes to read it now and then.
/// </summary>
public sealed class BountyHuntGoal : BotGoal
{
    private const int MinFightingSkill = 70;

    // A target this much stronger is left to someone else.
    private const int MaxSkillGap = 15;

    public override string Name => "Охота за головой";

    public override string[] News => ["Получил награду за голову, вот это дело!", "Выслеживал преступника полдня."];

    private static bool Fit(PlayerMobile bot) =>
        BotCombatStyles.FightingSkill(bot) >= MinFightingSkill && bot.Hits >= bot.HitsMax * 0.8 &&
        bot is not BotMobile { IsPk: true } && !BotSocialRules.IsOutlaw(bot);

    private static MahaonBountyBoard Board(PlayerMobile bot) =>
        BotSocialRules.TownFor(bot) is { } city ? WorldCatalog.GetPlaced<MahaonBountyBoard>(city) : null;

    private static BotMobile Target(BotBrain brain)
    {
        var bot = brain.Bot;
        return brain.KnownBounty is BotMobile { Deleted: false, Alive: true } target && target == BountyHunterSystem.CurrentTarget &&
               target != bot && target.Map == bot.Map && !BotSocialRules.IsFriend(bot, target) &&
               BotCombatStyles.FightingSkill(target) <= BotCombatStyles.FightingSkill(bot) + MaxSkillGap
            ? target
            : null;
    }

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (!Fit(bot))
        {
            return 0;
        }

        var bold = BotBrain.Trait((byte)(100 - brain.Caution));
        if (Target(brain) != null)
        {
            return 0.5 + BotBrain.Trait(brain.Greed) * 0.2 + bold * 0.3;
        }

        // Not read, or read about someone already dead: look at the board again.
        return brain.KnownBounty != BountyHunterSystem.CurrentTarget && BountyHunterSystem.CurrentTarget != null &&
               Board(bot) != null
            ? 0.2 + bold * 0.2
            : 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Target(brain) is { } target)
        {
            BotSpeech.SayText(brain.Bot, $"{target.Name}, за твою голову назначена награда!");
            return [new GoToAction(target, 6, $"за головой {target.Name}"), new BountyFightAction(target)];
        }

        var board = Board(brain.Bot);
        return board == null ? null : [new GoToAction(board.Map, board.Location, 2, "к доске розыска"), new UseBoardAction(board)];
    }
}

/// <summary>Fights the outlaw until one of them falls or it gets away.</summary>
public sealed class BountyFightAction : BotAction
{
    private const long MaxMs = 5 * 60_000;
    private const int LoseRange = 18;

    private readonly Mobile _target;
    private long _until;

    public BountyFightAction(Mobile target) => _target = target;

    public override void Start(BotBrain brain) => _until = Core.TickCount + MaxMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_target.Deleted || !_target.Alive)
        {
            brain.KnownBounty = null;
            return BotActionResult.Done(2000);
        }

        if (Core.TickCount - _until >= 0 || _target.Map != bot.Map || !bot.InRange(_target, LoseRange))
        {
            return BotActionResult.Failed();
        }

        if (brain.Combat.Opponent != _target)
        {
            BotCombat.Engage(brain, _target);
        }

        return BotActionResult.Running(1000);
    }

    public override string Describe(BotBrain brain) => $"Сражается с {_target.Name}";
}
