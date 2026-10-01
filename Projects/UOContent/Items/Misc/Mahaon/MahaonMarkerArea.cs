using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     What kind of gameplay area this marks. Add new values here as new mechanics need
///     their own "is the player standing in a marked zone" check — MahaonMarkerAreaSystem
///     already handles any kind generically, nothing else needs to change to add one.
/// </summary>
public enum MahaonMarkerAreaKind
{
    Graveyard // MahaonGraveyardDigSystem — Pandora's Box digging
}

[SerializationGenerator(0, false)]
public partial class MahaonMarkerArea : Item
{
    [SerializableField(0)]
    private MahaonMarkerAreaKind _kind;

    [SerializableField(1)]
    private Map _areaMap;

    [SerializableField(2)]
    private Rectangle2D _bounds;

    [SerializableField(3)]
    private string _label; // free-text, GM-chosen — purely for telling areas of the same Kind apart in the list gump

    [Constructible]
    public MahaonMarkerArea(MahaonMarkerAreaKind kind, Map map, Rectangle2D bounds, string label) : base(0x1F14)
    {
        _kind = kind;
        _areaMap = map;
        _bounds = bounds;
        _label = label;

        Movable = false;
        Visible = false;
        // Deliberately never placed on a map (no MoveToWorld) — same "real persisted
        // Item, purely an object reference, not world scenery" pattern as
        // MahaonGuildBankContainer/MahaonRaidMarker elsewhere in Mahaon.
    }

    public bool Contains(Mobile from) =>
        from?.Map != null && from.Map == AreaMap && Bounds.Contains(from.Location);
}
