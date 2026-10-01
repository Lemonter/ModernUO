using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// A sociable fighter gathers bots standing around in town into a group and leads it into a
/// dungeon — prey none of them would take alone. Prefers its own guild mates.
/// </summary>
public sealed class LeadGroupGoal : BotGoal
{
    private const int RecruitRange = 20;
    private const int SearchRange = 500;

    public override string Name => "Сбор отряда";

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (brain.Group != null || BotCombatStyles.FightingSkill(bot) < 50 || bot.Hits < bot.HitsMax * 0.9 ||
            BotSocialRules.IsOutlaw(bot))
        {
            return 0;
        }

        return 0.25 + BotBrain.Trait(brain.Sociability) * 0.4 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.15 -
               brain.Fatigue * 0.5;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var recruits = new List<BotMobile>();

        foreach (var m in bot.Map.GetMobilesInRange<BotMobile>(bot.Location, RecruitRange))
        {
            if (m == bot || m.Brain is not { Registered: true, Group: null } other || !m.Alive ||
                BotSocialRules.IsOutlaw(m) || BotCombatStyles.FightingSkill(m) < 30 ||
                other.Goal is LeadGroupGoal || other.Combat.Opponent != null)
            {
                continue;
            }

            // Guild mates go to the front of the queue.
            if (BotSocialRules.IsFriend(bot, m))
            {
                recruits.Insert(0, m);
            }
            else
            {
                recruits.Add(m);
            }
        }

        if (recruits.Count < 2)
        {
            return null;
        }

        var group = new BotGroup(bot);
        foreach (var m in recruits)
        {
            if (group.Members.Count >= BotGroup.MaxSize)
            {
                break;
            }

            if (group.TryAdd(m))
            {
                BotSpeech.Say(m, BotTopic.Greeting, bot, 0.5);
            }
        }

        var spot = HuntingAtlas.Pick(bot.Map, bot.Location, SearchRange, group.MaxPreyFame(), preferDungeon: true);
        if (spot == null)
        {
            group.Disband();
            return null;
        }

        BotSpeech.Say(bot, BotTopic.GroupForming, chance: 1.0);
        return [new GoToAction(spot.Map, spot.Center, 4, "ведёт отряд"), new GroupHuntAction(group, spot)];
    }
}

/// <summary>Hunts with the group, and breaks it up when done.</summary>
public sealed class GroupHuntAction : BotAction
{
    private readonly BotGroup _group;
    private readonly HuntAction _hunt;

    public GroupHuntAction(BotGroup group, HuntSpot spot)
    {
        _group = group;
        _hunt = new HuntAction(spot, 10 + group.Members.Count * 3, group.MaxPreyFame());
    }

    public override void Start(BotBrain brain) => _hunt.Start(brain);

    public override BotActionResult Tick(BotBrain brain) => _group.Disbanded ? BotActionResult.Done() : _hunt.Tick(brain);

    public override void Stop(BotBrain brain)
    {
        _hunt.Stop(brain);
        BotSpeech.Say(brain.Bot, BotTopic.GroupDone, chance: 0.8);
        _group.Disband();
    }

    public override string Describe(BotBrain brain) => $"Ведёт отряд ({_group.Members.Count}): {_hunt.Describe(brain)}";
}

/// <summary>A group member does what the group does; nothing else wins while the group stands.</summary>
public sealed class FollowGroupGoal : BotGoal
{
    public override string Name => "В отряде";

    public override double Score(BotBrain brain) =>
        brain.Group is { Disbanded: false } group && group.Leader != brain.Bot ? 2.0 : 0;

    public override List<BotAction> Plan(BotBrain brain) =>
        brain.Group is { Disbanded: false } group ? [new FollowLeaderAction(group)] : null;
}
