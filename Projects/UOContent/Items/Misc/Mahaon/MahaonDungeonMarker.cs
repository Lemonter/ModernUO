using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonDungeonMarker : Item
{
    [SerializableField(0)]
    private string _dungeonName;

    private static readonly List<MahaonDungeonMarker> AllMarkers = new();

    [Constructible]
    public MahaonDungeonMarker(string dungeonName = "Despise") : base(0x1F14)
    {
        Movable = false;
        Visible = false; // invisible to regular players — GMs still see it via [ItemID etc
        _dungeonName = dungeonName;
        Name = $"маркер данжа: {dungeonName}";

        AllMarkers.Add(this);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        AllMarkers.Add(this);
    }

    public override void OnDelete()
    {
        AllMarkers.Remove(this);
        base.OnDelete();
    }

    /// <summary>Finds a placed marker matching this dungeon name (case-insensitive) —
    /// returns null if nobody's placed one there yet.</summary>
    public static MahaonDungeonMarker Find(string dungeonName)
    {
        foreach (var marker in AllMarkers)
        {
            if (!marker.Deleted && string.Equals(marker._dungeonName, dungeonName, System.StringComparison.OrdinalIgnoreCase))
            {
                return marker;
            }
        }

        return null;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        from.SendMessage(0x59, $"Маркер данжа: {_dungeonName} ({X}, {Y}, {Z} — {Map})");
    }
}
