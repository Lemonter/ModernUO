using Server.Engines.Craft;
using Server.Items;

namespace Server.Systems.Bots;

/// <summary>Buys a blueprint at the blueprint shop's price and learns it — what the shop gump does
/// for a player.</summary>
public sealed class BuyBlueprintAction : BotAction
{
    private readonly Recipe _recipe;

    public BuyBlueprintAction(Recipe recipe) => _recipe = recipe;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.HasRecipe(_recipe))
        {
            return BotActionResult.Done();
        }

        var price = MahaonBlueprint.GetPrice(_recipe);
        if (bot.Backpack?.ConsumeTotal(typeof(Gold), price) != true)
        {
            return BotActionResult.Failed();
        }

        bot.AcquireRecipe(_recipe);
        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => "Изучает чертёж";
}
