using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Systems.MahaonAi;

/// <summary>
///     Scaffold for talking to a locally-running LM Studio instance (or anything else
///     exposing the same OpenAI-compatible /v1/chat/completions endpoint — llama.cpp
///     server, Ollama's OpenAI-compat mode, etc all work the same way).
///
///     Deliberately NOT wired into anything yet — no machine to actually test against
///     right now. The important design decisions are already made so this can be
///     dropped in and just... work, once a real endpoint exists:
///
///     - Fire-and-forget from the caller's perspective: GenerateAsync returns immediately
///       usable via a callback, the caller supplies its OWN fallback text to use if the
///       request fails/times out/AI is disabled entirely — nothing ever blocks waiting on
///       this, and nothing ever crashes/hangs the game if LM Studio isn't running.
///     - The HTTP call itself runs on a background thread (normal async/await) — but the
///       callback that actually TOUCHES game state (mobiles, timers, quest text) is
///       marshaled back onto the main game thread via Timer.DelayCall(TimeSpan.Zero, ...),
///       the same safe pattern used elsewhere in this codebase for async-to-sync
///       bridging. ModernUO's game logic is single-threaded; nothing from this class
///       should ever touch a Mobile/Item directly from the HTTP continuation itself.
///     - A tiny concurrency gate (SemaphoreSlim, default 1) — a local 7-8B model on a
///       single consumer GPU genuinely can't serve more than one or two requests at a
///       time without each one slowing down badly; queuing here keeps that honest rather
///       than letting every bot/quest fire a request at once and all of them crawl.
///     - Configured via ServerConfiguration (settings file), same pattern as
///       Engines.Help.PageDiscord elsewhere in this codebase — empty/unset URL means
///       "feature disabled", IsEnabled reflects that so callers can skip the whole thing
///       cheaply.
///
///     Intended first use: MayorQuestSystem — replace the flavor TEXT of a quest (why
///     the mayor wants these creatures dead, what he says) with something AI-generated,
///     while the MECHANICS (target type, kill count, gold reward) stay exactly as
///     server-rolled as they are now. The AI never decides anything that affects game
///     balance, only how it's described.
/// </summary>
public static class MahaonAiTextGenerator
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(MahaonAiTextGenerator));
    private static HttpClient _httpClient;
    private static string _baseUrl;
    private static string _model;
    private static readonly SemaphoreSlim ConcurrencyGate = new(1, 1);

    public static void Configure()
    {
        // Empty by default — nothing calls out anywhere until this is actually set in
        // the server config file. LM Studio's own default local port is 1234; adjust to
        // whatever your actual instance uses.
        _baseUrl = ServerConfiguration.GetOrUpdateSetting("mahaonAi.baseUrl", "");
        _model = ServerConfiguration.GetOrUpdateSetting("mahaonAi.model", "local-model");

        var timeoutSeconds = ServerConfiguration.GetOrUpdateSetting("mahaonAi.timeoutSeconds", 12);

        if (IsEnabled)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            _logger.Information($"MahaonAi text generation enabled — {_baseUrl}, model '{_model}'.");
        }
        else
        {
            _logger.Information("MahaonAi text generation disabled (no mahaonAi.baseUrl configured).");
        }
    }

    public static bool IsEnabled => !string.IsNullOrEmpty(_baseUrl);

    /// <summary>
    ///     Fire-and-forget: kicks off the request in the background, calls onResult on
    ///     the MAIN GAME THREAD (safe to touch Mobiles/Items/Timers inside it) once a
    ///     result is available — either the AI's actual text, or fallbackText if
    ///     anything went wrong (disabled, timeout, malformed response, model refused,
    ///     whatever). onResult ALWAYS fires exactly once, synchronously-from-the-caller's-
    ///     perspective-never — always async, even in the disabled/instant-fallback case,
    ///     so callers can't accidentally rely on ordering.
    /// </summary>
    public static void GenerateAsync(string systemPrompt, string userPrompt, string fallbackText, Action<string> onResult)
    {
        if (!IsEnabled)
        {
            Timer.DelayCall(TimeSpan.Zero, () => onResult(fallbackText));
            return;
        }

        _ = RunRequestAsync(systemPrompt, userPrompt, fallbackText, onResult);
    }

    private static async Task RunRequestAsync(string systemPrompt, string userPrompt, string fallbackText, Action<string> onResult)
    {
        string result = fallbackText;

        var gotSlot = await ConcurrencyGate.WaitAsync(TimeSpan.FromSeconds(5));

        if (!gotSlot)
        {
            // Already at capacity and stayed that way for 5s — bail to the fallback
            // rather than pile up an unbounded queue of waiting quest-givers.
            _logger.Warning("MahaonAi request skipped — concurrency gate busy for 5s.");
            Timer.DelayCall(TimeSpan.Zero, () => onResult(fallbackText));
            return;
        }

        try
        {
            var request = new ChatCompletionRequest
            {
                Model = _model,
                Messages =
                [
                    new ChatMessage { Role = "system", Content = systemPrompt },
                    new ChatMessage { Role = "user", Content = userPrompt }
                ],
                MaxTokens = 200,
                Temperature = 0.9
            };

            using var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request);

            if (response.IsSuccessStatusCode)
            {
                var parsed = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
                var text = parsed?.Choices?.Length > 0 ? parsed.Choices[0].Message?.Content?.Trim() : null;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    result = text;
                }
                else
                {
                    _logger.Warning("MahaonAi response had no usable text — using fallback.");
                }
            }
            else
            {
                _logger.Warning($"MahaonAi request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            // Network unreachable, timeout, malformed JSON, whatever — LM Studio simply
            // not running is the expected common case here, log at a low level rather
            // than alarming every time someone talks to a quest-giver with it off.
            _logger.Warning($"MahaonAi request error (falling back to default text): {ex.Message}");
        }
        finally
        {
            ConcurrencyGate.Release();
        }

        var finalResult = result;
        Timer.DelayCall(TimeSpan.Zero, () => onResult(finalResult));
    }

    // -- OpenAI-compatible wire format — minimal, only what LM Studio actually needs ----

    private class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public ChatMessage[] Messages { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    private class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage Message { get; set; }
    }
}
