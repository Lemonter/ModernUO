using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Placed by hand at whatever counts as "the city bank" (or any raid target point) —
///     MahaonRaidSpawner finds the nearest one of these to know where its raiders should
///     head for and start burning, instead of the raid system's old hardcoded coordinate
///     list. Purely a marker, no game logic of its own beyond existing to be found.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonRaidMarker : Item
{
    [SerializableField(0)]
    private string _cityName = "Безымянный город";

    [Constructible]
    public MahaonRaidMarker() : base(0x1876) // small flag-ish graphic, purely a GM marker
    {
        Movable = false;
        Visible = false;
        Name = "Метка набега";
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);
        LabelTo(from, $"Метка набега: {_cityName}");
    }
}
