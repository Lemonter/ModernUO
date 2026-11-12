using ModernUO.Serialization;
// ReSharper disable once RedundantUsingDirective — GetDistanceToSqrt (Utility.cs, namespace
// Server) is an extension method; despite Server.Items nesting under Server, extension
// method lookup didn't pick it up without this being explicit.
using Server;

namespace Server.Items;

/// <summary>
///     The "special block, spawn everything at once" requested separately from the normal
///     Spawner — a GM places one of these at the spawn point, sets the mob type name and
///     count, then double-clicks to trigger. Finds the nearest MahaonRaidMarker to know
///     which city the raiders should head for (sets their Home there, same pattern the
///     automatic RaidEventSystem already uses) and spawns a batch of MahaonTownDefenders
///     there too so guards actually have someone to fight. Raiders/defenders exchange only
///     1-2 damage either way (MahaonRaidCombat) — this is a spectacle for farming guard
///     rank, not a real threat.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonRaidSpawner : Item
{
    [SerializableField(0)]
    private string _mobTypeName = "RaidBandit";

    [SerializableField(1)]
    private int _minCount = 6;

    [SerializableField(2)]
    private int _maxCount = 10;

    [SerializableField(3)]
    private int _defenderCount = 4;

    [SerializableField(4)]
    private int _markerSearchRange = 64;

    [Constructible]
    public MahaonRaidSpawner() : base(0x1F14) // campfire-ish graphic, purely a GM tool
    {
        Movable = false;
        Visible = false;
        Name = "Спаунер набега";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel < AccessLevel.GameMaster)
        {
            return;
        }

        var (raiderCount, cityLabel) = TriggerSpawn();
        from.SendMessage(
            raiderCount > 0 ? 0x59 : 0x22,
            raiderCount > 0 ? $"Набег запущен: {raiderCount} налётчиков на {cityLabel}." : "Набег не начат: рядом нет города с подходами, или на него уже идёт набег."
        );
    }

    /// <summary>
    ///     Набег из этой точки: лагерь — здесь, город — ближайший с подходами (его ворота и
    ///     банк), дальше всё как у набега по расписанию (RaidDirector). Тип моба и числа из
    ///     старых полей больше не используются — отряд задаёт сила стороны.
    /// </summary>
    public (int raiderCount, string cityLabel) TriggerSpawn()
    {
        var site = Systems.MahaonRaids.RaidSites.Nearest(Map, Location);
        var raid = Systems.MahaonRaids.RaidDirector.Start(site, null, -1, Location);
        return raid == null ? (0, "точки спауна") : (raid.Raiders.Count, site.City);
    }
}
