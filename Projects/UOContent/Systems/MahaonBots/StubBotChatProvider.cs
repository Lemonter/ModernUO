namespace Server.Systems.MahaonBots;

/// <summary>
///     Placeholder chat backend: picks a canned line based on activity/context. Zero cost,
///     zero external dependency — exists purely so the rest of the bot system (and anything
///     that calls BotChat.RequestLine) already works end-to-end before the LLM is wired in.
/// </summary>
public class StubBotChatProvider : IBotChatProvider
{
    private static readonly string[] IdleLines =
    [
        "*смотрит вдаль*",
        "Ещё один спокойный день.",
        "..."
    ];

    private static readonly string[] GatheringLines =
    [
        "Эта жила выглядит многообещающе.",
        "*продолжает копать*",
        "Почти набрал полную ходку."
    ];

    private static readonly string[] CraftingLines =
    [
        "Посмотрим, получится ли как надо.",
        "*удары молота*"
    ];

    private static readonly string[] DungeonLines =
    [
        "Держись рядом, тут неприятное место.",
        "Прикрывай фланг!",
        "Добыча тут неплохая."
    ];

    private static readonly string[] GreetingLines =
    [
        "Приветствую.",
        "Добрый день.",
        "Хлопотный денёк?"
    ];

    public void RequestLine(BotChatRequest request, BotChatCallback callback)
    {
        var pool = request.Speaker != null
            ? GreetingLines
            : request.CurrentActivity switch
            {
                BotActivity.Gathering                                     => GatheringLines,
                BotActivity.Crafting                                      => CraftingLines,
                BotActivity.DungeonCombat or BotActivity.TravelingToDungeon => DungeonLines,
                _                                                          => IdleLines
            };

        // Stub is synchronous and already on the game loop, so we can call back directly.
        // A real LLM provider would do its network/inference work off-loop and marshal
        // this callback via Core.LoopContext.Post(...).
        callback(request.Bot, pool.RandomElement());
    }
}
