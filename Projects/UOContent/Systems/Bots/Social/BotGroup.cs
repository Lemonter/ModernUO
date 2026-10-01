using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// A hunting group: a leader and a few bots that follow it, fight what it fights and heal each
/// other. Lives only as long as the leader's plan; a member that dies, wanders off or is deleted
/// just drops out.
/// </summary>
public sealed class BotGroup
{
    public const int MaxSize = 5;

    public BotGroup(BotMobile leader)
    {
        Leader = leader;
        Members.Add(leader);
        leader.Brain.Group = this;
    }

    public BotMobile Leader { get; }

    public List<BotMobile> Members { get; } = [];

    public bool Disbanded { get; private set; }

    public bool TryAdd(BotMobile bot)
    {
        if (Disbanded || Members.Count >= MaxSize || bot.Brain == null || bot.Brain.Group != null)
        {
            return false;
        }

        Members.Add(bot);
        bot.Brain.Group = this;
        bot.Brain.ClearPlan();
        return true;
    }

    public void Remove(BotMobile bot)
    {
        Members.Remove(bot);
        if (bot.Brain?.Group == this)
        {
            bot.Brain.Group = null;
        }

        if (bot == Leader)
        {
            Disband();
        }
    }

    public void Disband()
    {
        if (Disbanded)
        {
            return;
        }

        Disbanded = true;
        foreach (var m in Members)
        {
            if (m.Brain?.Group == this)
            {
                m.Brain.Group = null;
            }
        }

        Members.Clear();
    }

    /// <summary>The toughest prey the group takes on together: a share of every member's limit.</summary>
    public int MaxPreyFame()
    {
        var total = 0;
        foreach (var m in Members)
        {
            if (m.Alive && m.Brain != null)
            {
                total += BotCombatStyles.MaxPreyFame(m.Brain);
            }
        }

        return total * 6 / 10;
    }

    /// <summary>What the group is fighting right now: the leader's foe, or any member's.</summary>
    public Mobile CurrentFoe()
    {
        if (Leader.Brain?.Combat.Opponent is { Alive: true, Deleted: false } foe)
        {
            return foe;
        }

        foreach (var m in Members)
        {
            if (m.Brain?.Combat.Opponent is { Alive: true, Deleted: false } other)
            {
                return other;
            }
        }

        return null;
    }
}
