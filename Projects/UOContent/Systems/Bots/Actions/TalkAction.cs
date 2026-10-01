using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>A short conversation with someone standing close by. With another bot it is an
/// exchange of lines, and both come away less lonely.</summary>
public sealed class TalkAction : BotAction
{
    private const int TalkRange = 4;

    private readonly Mobile _partner;
    private int _lines;
    private int _maxLines;

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
            BotSpeech.Say(bot, _lines == 0 ? BotTopic.Greeting : BotTopic.SmallTalk, target: _partner);
        }
        else if (_partner is BotMobile { Brain: { } other } partnerBot)
        {
            partnerBot.Direction = partnerBot.GetDirectionTo(bot);
            BotSpeech.Say(partnerBot, BotTopic.SmallTalk, target: bot);
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
