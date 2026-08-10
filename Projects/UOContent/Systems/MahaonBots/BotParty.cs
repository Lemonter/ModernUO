using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

public enum PartyRole
{
    Tank,
    Healer,
    Dps
}

/// <summary>
///     A small group of bots that decided to run a dungeon together — real role slots now,
///     not just a headcount. Composition is fixed at 1 Tank, 1 Healer, 2 Dps. A party stays
///     in the "forming" state (gathered at RendezvousPoint, chatting) until every slot is
///     filled, then everyone travels to Destination together.
/// </summary>
public class BotParty
{
    public readonly List<PlayerMobile> Members = new();
    public readonly Dictionary<PlayerMobile, PartyRole> Roles = new();
    public DungeonTarget Destination;
    public Point3D RendezvousPoint;
    public Map RendezvousMap;

    private static readonly Dictionary<PartyRole, int> DesiredSlots = new()
    {
        [PartyRole.Tank] = 1,
        [PartyRole.Healer] = 1,
        [PartyRole.Dps] = 2
    };

    public bool IsAlive => Members.TrueForAll(m => !m.Deleted && m.Alive);

    public bool IsFull => Members.Count >= 4;

    public int FilledCount(PartyRole role)
    {
        var count = 0;
        foreach (var member in Members)
        {
            if (Roles.TryGetValue(member, out var r) && r == role)
            {
                count++;
            }
        }

        return count;
    }

    public bool HasOpenSlot(PartyRole role) => FilledCount(role) < DesiredSlots.GetValueOrDefault(role, 0);

    /// <summary>Which role(s) this bot could fill, in preference order — a Warrior tries
    /// Tank first, falls back to Dps if that slot's already taken.</summary>
    public static IEnumerable<PartyRole> EligibleRoles(PlayerMobile bot)
    {
        if (bot is not BotMobile botMobile)
        {
            yield break;
        }

        switch (botMobile.Archetype)
        {
            case BotArchetype.Warrior:
                yield return PartyRole.Tank;
                yield return PartyRole.Dps;
                break;
            case BotArchetype.Mage:
                yield return PartyRole.Healer;
                yield return PartyRole.Dps;
                break;
            case BotArchetype.Archer:
                yield return PartyRole.Dps;
                break;
            case BotArchetype.Trader:
                yield return PartyRole.Dps;
                break;
            // Crafter: no combat role, doesn't queue for dungeon parties at all.
        }
    }

    public bool TryClaimSlot(PlayerMobile bot, out PartyRole claimed)
    {
        foreach (var role in EligibleRoles(bot))
        {
            if (HasOpenSlot(role))
            {
                claimed = role;
                Members.Add(bot);
                Roles[bot] = role;
                return true;
            }
        }

        claimed = default;
        return false;
    }

    public void Add(PlayerMobile bot) => Members.Add(bot);

    public void Remove(PlayerMobile bot)
    {
        Members.Remove(bot);
        Roles.Remove(bot);
    }
}

/// <summary>
///     A dungeon a bot party can travel to. Coordinates are placeholders — swap in real
///     Mahaon dungeon entrances as they're decided.
/// </summary>
public readonly struct DungeonTarget
{
    public readonly string Name;
    public readonly Point3D Entrance;
    public readonly Map Map;

    public DungeonTarget(string name, Point3D entrance, Map map)
    {
        Name = name;
        Entrance = entrance;
        Map = map;
    }

    public static readonly DungeonTarget[] Known =
    [
        new DungeonTarget("Despise", new Point3D(5136, 1965, 0), Map.Felucca),
        new DungeonTarget("Deceit", new Point3D(4111, 434, 5), Map.Felucca),
        new DungeonTarget("Destard", new Point3D(1200, 3712, 5), Map.Felucca),
        new DungeonTarget("Covetous", new Point3D(2531, 3377, 0), Map.Felucca),
        new DungeonTarget("Shame", new Point3D(508, 1550, 0), Map.Felucca)
    ];
}
