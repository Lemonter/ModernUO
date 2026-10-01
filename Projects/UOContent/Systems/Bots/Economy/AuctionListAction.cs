using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonAuction;

namespace Server.Systems.Bots;

/// <summary>Puts whatever goods no vendor took up on the town auction.</summary>
public sealed class AuctionListAction : BotAction
{
    private readonly string _city;

    public AuctionListAction(string city) => _city = city;

    public override BotActionResult Tick(BotBrain brain)
    {
        var pack = brain.Bot.Backpack;
        if (pack == null)
        {
            return BotActionResult.Failed();
        }

        var goods = new List<Item>();
        foreach (var item in pack.Items)
        {
            if (BotGoods.IsForSale(item))
            {
                goods.Add(item);
            }
        }

        foreach (var item in goods)
        {
            var unit = BotGoods.AskingUnitPrice(item, _city);
            if (unit > 0)
            {
                AuctionHouseSystem.CreateListing(brain.Bot, item, unit * item.Amount, _city);
            }
        }

        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => $"Выставляет товар на аукцион ({_city})";
}
