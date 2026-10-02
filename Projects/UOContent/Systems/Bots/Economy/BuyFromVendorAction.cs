using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Buys from a vendor's stock through the vendor's own buy handler — price, stock and
/// payment exactly as a player's buy list.</summary>
public sealed class BuyFromVendorAction : BotAction
{
    private const int VendorRange = 4;

    private readonly BaseVendor _vendor;
    private readonly Type _type;
    private readonly int _amount;
    private readonly Func<GenericBuyInfo, bool> _match;

    /// <param name="match">Picks one entry among several of the same type — the seed entries
    /// differ only by the crop they are constructed with.</param>
    public BuyFromVendorAction(BaseVendor vendor, Type type, int amount, Func<GenericBuyInfo, bool> match = null)
    {
        _vendor = vendor;
        _type = type;
        _amount = amount;
        _match = match;
    }

    public static GenericBuyInfo FindStock(BaseVendor vendor, Type type, Func<GenericBuyInfo, bool> match = null)
    {
        foreach (var info in vendor.GetBuyInfo())
        {
            if (info is GenericBuyInfo gbi && gbi.Amount > 0 && type.IsAssignableFrom(gbi.Type) && match?.Invoke(gbi) != false)
            {
                return gbi;
            }
        }

        return null;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_vendor.Deleted || !_vendor.Alive || !bot.InRange(_vendor, VendorRange))
        {
            return BotActionResult.Failed();
        }

        var stock = FindStock(_vendor, _type, _match);
        if (stock?.GetDisplayEntity() is not { } display)
        {
            return BotActionResult.Failed();
        }

        bot.Direction = bot.GetDirectionTo(_vendor);
        bot.Say("купить");

        return _vendor.OnBuyItems(bot, [new BuyItemResponse(display.Serial, Math.Min(_amount, stock.Amount))])
            ? BotActionResult.Done(2000)
            : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => $"Покупает у {_vendor.Name}";
}

/// <summary>Takes gold out of the bank, standing at a banker.</summary>
public sealed class WithdrawGoldAction : BotAction
{
    private const int BankerRange = 12;

    private readonly Banker _banker;
    private readonly int _amount;

    public WithdrawGoldAction(Banker banker, int amount)
    {
        _banker = banker;
        _amount = amount;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_banker.Deleted || !bot.InRange(_banker, BankerRange))
        {
            return BotActionResult.Failed();
        }

        var amount = Math.Min(_amount, Banker.GetBalance(bot));
        if (amount <= 0)
        {
            return BotActionResult.Failed();
        }

        bot.Say("снять");
        return Banker.Withdraw(bot, amount) ? BotActionResult.Done(1500) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => "Снимает золото в банке";
}
