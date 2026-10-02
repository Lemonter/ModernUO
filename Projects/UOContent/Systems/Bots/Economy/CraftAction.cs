using Server.Items;
using Server.Engines.Craft;

namespace Server.Systems.Bots;

/// <summary>
/// A crafting session: pick the best craft available, make it through the craft engine, wait for
/// it to finish, repeat. Ends after a batch, or when nothing is craftable any more (materials or
/// tool gone).
/// </summary>
public sealed class CraftAction : BotAction
{
    private readonly CraftSystem _system;
    private readonly int _batch;

    private int _made;
    private bool _crafting;

    public CraftAction(CraftSystem system, int batch)
    {
        _system = system;
        _batch = batch;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        // The craft engine holds the action for the length of the craft animation.
        if (!bot.CanBeginAction<CraftSystem>())
        {
            return BotActionResult.Running(800);
        }

        if (_crafting)
        {
            _crafting = false;
            _made++;
        }

        if (_made >= _batch)
        {
            return BotActionResult.Done();
        }

        var tool = BotCrafting.FindTool(bot, _system);
        if (tool == null || !BotCrafting.TryPick(bot, _system, out var choice))
        {
            return _made > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        choice.Item.Craft(bot, _system, choice.ResourceType, tool);
        _crafting = !bot.CanBeginAction<CraftSystem>();

        return _crafting ? BotActionResult.Running(1500) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => $"Ремесло ({_system.MainSkill}): {_made}/{_batch}";
}

/// <summary>Cuts a tailor's bolts of cloth and hides with scissors, through their own
/// scissoring: bolts into cloth, hides into leather.</summary>
public sealed class CutMaterialsAction : BotAction
{
    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;
        if (pack?.FindItemByType<Scissors>() is not { } scissors)
        {
            return BotActionResult.Done();
        }

        var uncut = new System.Collections.Generic.List<Item>();
        foreach (var item in pack.Items)
        {
            if (item is BoltOfCloth or BaseHides or UncutCloth)
            {
                uncut.Add(item);
            }
        }

        foreach (var item in uncut)
        {
            ((IScissorable)item).Scissor(bot, scissors);
        }

        return BotActionResult.Done(uncut.Count > 0 ? 1500 : 0);
    }

    public override string Describe(BotBrain brain) => "Режет ткань и шкуры";
}
