using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/RisingTideData.cs).
///     ServUO's <c>PointsSystem</c> base does not exist here, so this is a plain static store of doubloons with its
///     own <see cref="Enabled" /> switch (default false).
///     The cargo kill hook is the High Seas <c>PlunderBeacon</c> (stage 4): destroying one hands
///     <see cref="Items.MaritimeCargo" /> to the winners, and <see cref="BlackMarketMerchant" /> buys that cargo
///     for doubloons. ServUO's ship crews (<c>PirateCrew</c>, <c>MerchantCrew</c>, <c>BaseShipCaptain</c>) still
///     do not exist, so they are not drop sources.
/// </summary>
public static class RisingTideData
{
    public const double CargoChance = 0.1;

    private static readonly Dictionary<Mobile, int> _points = new();

    private static RisingTidePersistence _persistence;

    /// <summary>Сезонный переключатель Rising Tide. По умолчанию выключен.</summary>
    public static bool Enabled { get; set; }

    public static bool InSeason => Enabled;

    public static void Configure()
    {
        _persistence = new RisingTidePersistence();
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m) => _points.Remove(m);

    public static int GetPoints(Mobile m) => m != null && _points.TryGetValue(m, out var points) ? points : 0;

    public static void AwardPoints(Mobile m, int amount)
    {
        if (m == null || amount <= 0)
        {
            return;
        }

        _points[m] = GetPoints(m) + amount;
    }

    public static bool DeductPoints(Mobile m, int amount)
    {
        if (m == null || amount <= 0 || !_points.TryGetValue(m, out var current))
        {
            return false;
        }

        var remaining = current - amount;

        if (remaining > 0)
        {
            _points[m] = remaining;
        }
        else
        {
            _points.Remove(m);
        }

        return true;
    }

    public static void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(Enabled);
        writer.Write(_points.Count);

        foreach (var (mobile, points) in _points)
        {
            writer.Write(mobile);
            writer.Write(points);
        }
    }

    public static void Deserialize(IGenericReader reader)
    {
        reader.ReadInt();

        Enabled = reader.ReadBool();

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var points = reader.ReadInt();

            if (m != null && points > 0)
            {
                _points[m] = points;
            }
        }
    }
}

public class RisingTidePersistence : GenericPersistence
{
    public RisingTidePersistence() : base("RisingTide", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => RisingTideData.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => RisingTideData.Deserialize(reader);
}
