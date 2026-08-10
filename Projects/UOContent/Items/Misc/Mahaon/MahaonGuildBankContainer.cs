using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     The real, persisted storage behind Systems.MahaonBots.GuildBank — a plain Item with
///     Movable/Visible off and never placed on a map (same "exists only as an object
///     reference, not world scenery" pattern as MahaonRaidMarker), but unlike the old
///     GuildBank implementation (a bare `new Bag()` with no memory of which guild it
///     belonged to), this one carries its own guild name as a real SerializableField — so
///     after a restart, GuildBank can find it again by scanning World.Items instead of
///     losing track and creating an orphaned duplicate every time.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonGuildBankContainer : Container
{
    [SerializableField(0)]
    private string _ownerGuildName;

    [Constructible]
    public MahaonGuildBankContainer() : base(0xE76)
    {
        Movable = false;
        Visible = false;
    }
}
