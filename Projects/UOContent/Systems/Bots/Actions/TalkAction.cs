using Server.Misc;
using Server.Mobiles;
using Server.Systems.MahaonCities;

namespace Server.Systems.Bots;

/// <summary>A short conversation with someone standing close by. With another bot it is an
/// exchange of lines, and both come away less lonely.</summary>
public sealed class TalkAction : BotAction
{
    private const int TalkRange = 4;
    private const double RaidTopicChance = 0.3;

    private readonly Mobile _partner;
    private int _lines;
    private int _maxLines;

    // Начатая тема ждёт ответа собеседника: о набеге отвечают о набеге, а не о погоде.
    private string _pendingReply;
    private Emote? _pendingEmote;

    public TalkAction(Mobile partner) => _partner = partner;

    public override void Start(BotBrain brain) => _maxLines = Utility.RandomMinMax(2, 5);

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_partner.Deleted || !_partner.Alive || _partner.Map != bot.Map || !bot.InRange(_partner, TalkRange))
        {
            return _lines > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        bot.Direction = bot.GetDirectionTo(_partner);

        // Alternate: even lines are ours, odd lines the partner's (when the partner is a bot).
        if (_lines % 2 == 0)
        {
            if (_lines == 0)
            {
                BotSpeech.Say(bot, BotTopic.Greeting, target: _partner);
            }
            else if (Utility.RandomDouble() < RaidTopicChance && _partner.GetBrain() != null &&
                     TownTalk.TryRaidTalk(BotSocialRules.TownFor(bot)?.Name, out var opener, out var reply, out var emote) &&
                     BotSpeech.SayText(bot, opener))
            {
                _pendingReply = reply;
                _pendingEmote = emote;
            }
            else
            {
                BotSpeech.Chat(bot, _partner);
            }
        }
        else if (_partner.GetBrain() is { } other && _partner is var partnerBot)
        {
            partnerBot.Direction = partnerBot.GetDirectionTo(bot);
            if (_pendingReply != null)
            {
                if (_pendingEmote is { } emote)
                {
                    MahaonEmotes.Do(partnerBot, emote);
                }

                BotSpeech.SayText(partnerBot, _pendingReply);
                _pendingReply = null;
                _pendingEmote = null;
            }
            else
            {
                BotSpeech.Chat(partnerBot, bot);
            }

            other.OnSocialized(0.3);
        }

        if (++_lines >= _maxLines)
        {
            BotSpeech.Say(bot, BotTopic.Farewell, target: _partner);
            brain.OnSocialized(0.7);
            return BotActionResult.Done(1500);
        }

        return BotActionResult.Running(Utility.RandomMinMax(2500, 5000));
    }

    public override string Describe(BotBrain brain) => $"Разговаривает с {_partner.Name}";
}
