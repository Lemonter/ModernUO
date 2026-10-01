using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Underworld/ExperimentalRoom/
// ExperimentalRoomController.cs) — the singleton that tracks the 24-hour per-player cooldown
// after an ExperimentalGem is destroyed (success, failure, or decay all count). One
// deliberate deviation: the real source never actually persists this table (Deserialize just
// reinitializes it empty despite Serialize writing a version int) — every restart silently
// resets everyone's cooldown. Since this shard restarts far more often than a live OSI
// server would, that reads as an oversight rather than a design choice, so the table is
// persisted here via a real [SerializableField] instead.
[SerializationGenerator(0, false)]
public partial class ExperimentalRoomController : Item
{
    private static ExperimentalRoomController _instance;
    public static ExperimentalRoomController Instance => _instance;

    [SerializableField(0)]
    private Dictionary<Mobile, DateTime> _table;

    [Constructible]
    public ExperimentalRoomController() : base(7107)
    {
        _table = new Dictionary<Mobile, DateTime>();
        Visible = false;
        Movable = false;

        _instance = this;
    }

    public static void AddToTable(Mobile from)
    {
        if (from == null || Instance == null)
        {
            return;
        }

        Instance._table[from] = Core.Now + TimeSpan.FromHours(24);
    }

    public static bool IsInCooldown(Mobile from)
    {
        Defrag();

        return Instance?._table.ContainsKey(from) == true;
    }

    private static void Defrag()
    {
        if (Instance == null)
        {
            return;
        }

        List<Mobile> toRemove = null;

        foreach (var kvp in Instance._table)
        {
            if (kvp.Value <= Core.Now)
            {
                (toRemove ??= new List<Mobile>()).Add(kvp.Key);
            }
        }

        if (toRemove == null)
        {
            return;
        }

        foreach (var m in toRemove)
        {
            Instance._table.Remove(m);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization() => _instance = this;
}
