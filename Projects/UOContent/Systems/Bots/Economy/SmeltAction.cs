using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Systems.Bots;

/// <summary>
/// Smelts every ore pile in the pack at a forge, through the ore's own double-click and smelt
/// target — the player's path, skill checks and burnt ore included. One pile per tick, at the
/// pace of a player clicking through them.
/// </summary>
public sealed class SmeltAction : BotAction
{
    private const int ForgeRange = 2;

    private readonly object _forge;
    private readonly Point3D _forgeLocation;

    // Плавка может отказать, ничего не потратив (навык ниже нужного, «не похоже на печь»), и
    // тогда следующий тик брал ту же стопку — бесконечно, раз в полторы секунды.
    private Item _lastOre;
    private int _lastAmount;
    private int _stalls;
    private const int MaxStalls = 2;

    public SmeltAction(object forge, Point3D forgeLocation)
    {
        _forge = forge;
        _forgeLocation = forgeLocation;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_forge is Item { Deleted: true } || !Utility.InRange(bot.Location, _forgeLocation, ForgeRange))
        {
            return BotActionResult.Failed();
        }

        var ore = bot.Backpack?.FindItemByType<MahaonOre>();
        if (ore == null)
        {
            return BotActionResult.Done();
        }

        if (ore == _lastOre && !ore.Deleted && ore.Amount == _lastAmount)
        {
            if (++_stalls >= MaxStalls)
            {
                return BotActionResult.Failed();
            }
        }
        else
        {
            _stalls = 0;
        }

        _lastOre = ore;
        _lastAmount = ore.Amount;

        bot.Direction = bot.GetDirectionTo(_forgeLocation);
        ore.OnDoubleClick(bot);

        var target = bot.Target;
        if (target == null)
        {
            return BotActionResult.Failed();
        }

        target.Invoke(bot, _forge);
        return BotActionResult.Running(1500);
    }

    public override string Describe(BotBrain brain) => "Плавит руду";
}
