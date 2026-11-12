using Server.Mobiles;
using Server.Systems.MahaonAuction;

namespace Server.Systems.Bots;

/// <summary>
/// Buys one lot at the town auction through the same call the auction gump makes for a player:
/// gold from the pack, the goods into it, the seller paid less the house's cut and the town's tax.
/// </summary>
public sealed class BuyAtAuctionAction : BotAction
{
    private readonly int _listingId;
    private readonly long _expectedPrice;

    public BuyAtAuctionAction(int listingId, long expectedPrice)
    {
        _listingId = listingId;
        _expectedPrice = expectedPrice;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        if (brain.Bot is not PlayerMobile buyer)
        {
            return BotActionResult.Failed();
        }

        // Пока бот шёл, лот могли купить или снять, а цену — поменять: тогда план строится заново.
        if (!AuctionHouseSystem.TryGetListing(_listingId, out var listing) || listing.Price != _expectedPrice)
        {
            return BotActionResult.Failed();
        }

        var item = listing.Item;
        if (AuctionHouseSystem.TryBuy(buyer, _listingId) != AuctionBuyResult.Success)
        {
            return BotActionResult.Failed();
        }

        // Купленное для себя — не добыча на продажу.
        brain.ForgetLoot(item);
        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => "Покупает на аукционе";
}
