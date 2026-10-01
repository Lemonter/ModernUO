namespace Server.Systems.MahaonBots;

/// <summary>
///     What a bot is currently "doing". Drives which handler BotController dispatches to
///     on the bot's next scheduled decision tick.
/// </summary>
public enum BotActivity
{
    Idle, // standing around / AFK — the majority state for "just vibing" bots
    Wandering, // aimless local movement
    TravelingToGather, // walking out to a gathering spot
    Gathering, // mining, lumberjacking, etc.
    ReturningFromGather, // walking back to town to smelt/craft what was gathered
    Crafting, // turning gathered resources into goods
    TravelingToMarket, // walking to a vendor/market to sell
    Selling, // listing goods for sale
    FormingParty, // looking for/joining a dungeon party
    TravelingToDungeon, // party is en route
    DungeonCombat, // party is fighting inside a dungeon
    ReturningHome, // heading back after a dungeon run
    Hunting, // PK bots: actively seeking out a target to attack
    CityTraveling, // using a travel stone to relocate to another city
    BankingTrip, // walking to the local banker to deposit/withdraw before spending
    TravelingToHunt, // non-PK: heading out to a remembered (or scouted) monster spot
    SoloHunting, // fighting whatever's around at the hunting spot
    ReturningFromHunt // walking back to town after a hunting trip
}

public static class BotActivityExtensions
{
    /// <summary>
    ///     Идёт ли бот куда-то с этим занятием. Быстрый тик движения (PollMovement)
    ///     шагает только за такими; всем прочим достаточно медленного тика решений.
    ///
    ///     Держится здесь, вплотную к самому перечислению, чтобы новое занятие нельзя было
    ///     завести, забыв о нём: забытое занятие означало неподвижного бота, который при
    ///     этом считает, что идёт.
    /// </summary>
    public static bool IsTravel(this BotActivity activity) => activity is
        BotActivity.TravelingToGather or
        BotActivity.ReturningFromGather or
        BotActivity.TravelingToDungeon or
        BotActivity.ReturningHome or
        BotActivity.TravelingToMarket or
        BotActivity.BankingTrip or
        BotActivity.CityTraveling or
        BotActivity.TravelingToHunt or
        BotActivity.ReturningFromHunt;
}
