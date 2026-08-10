using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     What a bot needs from a chat backend to say something contextual. Implement this
///     against the local LLM later (e.g. an Ollama/llama.cpp HTTP call on the RTX 4060 box)
///     and swap it in via <see cref="BotChat.Provider" /> — nothing else in the bot system
///     needs to change.
/// </summary>
public interface IBotChatProvider
{
    /// <summary>
    ///     Requests a line of dialogue for the bot. Implementations should be async-friendly
    ///     internally but this entry point is fire-and-forget from the caller's perspective —
    ///     see <see cref="BotChat.RequestLine" /> for how results get delivered back
    ///     to the game loop safely.
    /// </summary>
    void RequestLine(BotChatRequest request, BotChatCallback callback);
}

/// <summary>
///     Context handed to the chat provider so it can produce something relevant instead of
///     generic filler — who's talking, what they're doing, and what was said to them (if
///     anything).
/// </summary>
public readonly struct BotChatRequest
{
    public readonly PlayerMobile Bot;
    public readonly BotActivity CurrentActivity;
    public readonly Mobile Speaker; // who the bot is responding to, if any
    public readonly string IncomingMessage; // what the speaker said, if any

    public BotChatRequest(PlayerMobile bot, BotActivity currentActivity, Mobile speaker, string incomingMessage)
    {
        Bot = bot;
        CurrentActivity = currentActivity;
        Speaker = speaker;
        IncomingMessage = incomingMessage;
    }
}

/// <summary>
///     Delivers a chat provider's result back to the caller. Real (LLM-backed) providers
///     will generate this off the main loop — implementations MUST marshal the call back
///     via <c>Core.LoopContext.Post(...)</c> before touching the bot Mobile, same rule as
///     any other background work per the threading model.
/// </summary>
public delegate void BotChatCallback(PlayerMobile bot, string line);

/// <summary>
///     Static access point for the active chat provider. Defaults to a stub that picks a
///     canned line per activity — swap <see cref="Provider" /> for a real implementation
///     once the local LLM integration exists.
/// </summary>
public static class BotChat
{
    public static IBotChatProvider Provider { get; set; } = new StubBotChatProvider();

    public static void RequestLine(BotChatRequest request, BotChatCallback callback) =>
        Provider.RequestLine(request, callback);
}
