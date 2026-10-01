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

    public BotGroup(PlayerMobile leader)
    {
        Leader = leader;
        Members.Add(leader);
        leader.GetBrain().Group = this;
    }

    public PlayerMobile Leader { get; }

    public List<PlayerMobile> Members { get; } = [];

    public bool Disbanded { get; private set; }

    public bool TryAdd(PlayerMobile bot)
    {
        if (Disbanded || Members.Count >= MaxSize || bot.GetBrain() == null || bot.GetBrain().Group != null)
        {
            return false;
        }

        Members.Add(bot);
        bot.GetBrain().Group = this;
        bot.GetBrain().ClearPlan();
        return true;
    }

    public void Remove(PlayerMobile bot)
    {
        Members.Remove(bot);
        if (bot.GetBrain()?.Group == this)
        {
            bot.GetBrain().Group = null;
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
            if (m.GetBrain()?.Group == this)
            {
                m.GetBrain().Group = null;
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
            if (m.Alive && m.GetBrain() != null)
            {
                total += BotCombatStyles.MaxPreyFame(m.GetBrain());
            }
        }

        return total * 6 / 10;
    }

    /// <summary>What the group is fighting right now: the leader's foe, or any member's.</summary>
    public Mobile CurrentFoe()
    {
        if (Leader.GetBrain()?.Combat.Opponent is { Alive: true, Deleted: false } foe)
        {
            return foe;
        }

        foreach (var m in Members)
        {
            if (m.GetBrain()?.Combat.Opponent is { Alive: true, Deleted: false } other)
            {
                return other;
            }
        }

        return null;
    }
}
