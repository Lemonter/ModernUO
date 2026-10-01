using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Sells everything the vendor buys from the bot's goods, through the vendor's own sell
/// handler — the same prices and rules a player's sell list gets.</summary>
public sealed class SellToVendorAction : BotAction
{
    private const int VendorRange = 4;

    // BaseVendor refuses a sell list longer than its own cap (500).
    private const int MaxSellPerVisit = 500;

    private readonly BaseVendor _vendor;

    public SellToVendorAction(BaseVendor vendor) => _vendor = vendor;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;

        if (pack == null || _vendor.Deleted || !_vendor.Alive || !bot.InRange(_vendor, VendorRange))
        {
            return BotActionResult.Failed();
        }

        var sellInfo = _vendor.GetSellInfo();
        var list = new List<SellItemResponse>();

        foreach (var item in pack.Items)
        {
            if (!BotGoods.IsForSale(item))
            {
                continue;
            }

            foreach (var info in sellInfo)
            {
                if (info.IsSellable(item))
                {
                    list.Add(new SellItemResponse(item, item.Amount));
                    break;
                }
            }

            if (list.Count >= MaxSellPerVisit)
            {
                break;
            }
        }

        if (list.Count == 0)
        {
            return BotActionResult.Done();
        }

        bot.Direction = bot.GetDirectionTo(_vendor);
        bot.Say("продать");
        _vendor.OnSellItems(bot, list);
        return BotActionResult.Done(2000);
    }

    public override string Describe(BotBrain brain) => $"Продаёт: {_vendor.Name}";
}
