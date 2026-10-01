using System.Collections.Generic;
using Server.Network;

namespace Server.Systems.MahaonQuests;

public enum QuestMarkerState
{
    None,
    Available,  // yellow "!" — has something new to offer this player
    InProgress, // grey "?" — player has an active quest here, not yet turned in
    Complete    // yellow "?" — ready to turn in
}

/// <summary>
///     A single, generic place any quest-giving system (mayor quests, bounty board,
///     crafting guild orders, whatever comes next) can flag "this NPC has something for
///     this specific player right now" — the client reads this per-player, per-mobile
///     state to draw the actual marker over the NPC's head (see
///     ClassicUO ...MahaonQuestMarker rendering, driven by a packet sent whenever a
///     state here changes). Deliberately per-(player, NPC) rather than a single global
///     flag on the NPC — two different players can see two different states on the same
///     NPC (one has an available quest, another already turned theirs in).
/// </summary>
public static class QuestMarkerRegistry
{
    private static readonly Dictionary<(Mobile player, Mobile npc), QuestMarkerState> States = new();

    public static void SetState(Mobile player, Mobile npc, QuestMarkerState state)
    {
        var key = (player, npc);

        if (state == QuestMarkerState.None)
        {
            States.Remove(key);
        }
        else
        {
            States[key] = state;
        }

        player.NetState?.SendQuestMarker(npc.Serial.Value, (byte)state);
    }

    public static QuestMarkerState GetState(Mobile player, Mobile npc) =>
        States.TryGetValue((player, npc), out var state) ? state : QuestMarkerState.None;

    /// <summary>Call once when a player comes within range of an NPC (e.g. on
    /// login/region-enter) to re-send whatever marker state they should already be
    /// seeing — the packet itself is fire-and-forget, nothing persists client-side
    /// between visibility refreshes on its own.</summary>
    public static void ResendVisible(Mobile player, IEnumerable<Mobile> nearbyNpcs)
    {
        foreach (var npc in nearbyNpcs)
        {
            var state = GetState(player, npc);

            if (state != QuestMarkerState.None)
            {
                player.NetState?.SendQuestMarker(npc.Serial.Value, (byte)state);
            }
        }
    }
}
