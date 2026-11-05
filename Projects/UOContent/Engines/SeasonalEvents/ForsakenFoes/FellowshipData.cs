using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

public enum FellowshipChain
{
    None,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight
}

/// <summary>
///     Ported from ServUO (Scripts/Services/PointsSystems/FellowshipData.cs, plus the file-backed state ServUO kept
///     in <c>Worker</c> and <c>FellowshipDonationBox</c>). ServUO's <c>PointsSystem</c> and <c>Persistence.Serialize</c>
///     files do not exist here, so everything is a plain static store persisted through one
///     <see cref="GenericPersistence" />: Fellowship Silver, the per-player chain progress, and the donation ledger.
/// </summary>
public static class FellowshipData
{
    private static readonly Dictionary<Mobile, int> _points = new();
    private static readonly Dictionary<Mobile, FellowshipChain> _chains = new();
    private static readonly Dictionary<Mobile, int> _donations = new();

    private static FellowshipPersistence _persistence;

    /// <summary>Сезонный переключатель The Fellowship. По умолчанию выключен.</summary>
    public static bool Enabled { get; set; }

    public static bool QuestContentGenerated { get; set; }

    public static bool InSeason => Enabled;

    public static void Configure()
    {
        _persistence = new FellowshipPersistence();
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m)
    {
        _points.Remove(m);
        _chains.Remove(m);
        _donations.Remove(m);
    }

    public static int GetPoints(Mobile m) => m != null && _points.TryGetValue(m, out var points) ? points : 0;

    public static void AwardPoints(Mobile m, int amount)
    {
        if (m == null || amount <= 0)
        {
            return;
        }

        _points[m] = GetPoints(m) + amount;
    }

    public static void AwardPoints(Mobile m, double amount) => AwardPoints(m, (int)amount);

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

    public static bool TryGetChain(Mobile m, out FellowshipChain chain)
    {
        chain = FellowshipChain.None;
        return m != null && _chains.TryGetValue(m, out chain);
    }

    public static void SetChain(Mobile m, FellowshipChain chain)
    {
        if (m != null)
        {
            _chains[m] = chain;
        }
    }

    public static int GetDonations(Mobile m) => m != null && _donations.TryGetValue(m, out var value) ? value : 0;

    public static void AddDonation(Mobile m, int amount)
    {
        if (m != null && amount > 0)
        {
            _donations[m] = GetDonations(m) + amount;
        }
    }

    public static void SetDonation(Mobile m, int amount)
    {
        if (m != null)
        {
            _donations[m] = amount;
        }
    }

    public static void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(Enabled);
        writer.Write(QuestContentGenerated);

        writer.Write(_points.Count);

        foreach (var (mobile, points) in _points)
        {
            writer.Write(mobile);
            writer.Write(points);
        }

        writer.Write(_chains.Count);

        foreach (var (mobile, chain) in _chains)
        {
            writer.Write(mobile);
            writer.Write((int)chain);
        }

        writer.Write(_donations.Count);

        foreach (var (mobile, amount) in _donations)
        {
            writer.Write(mobile);
            writer.Write(amount);
        }
    }

    public static void Deserialize(IGenericReader reader)
    {
        reader.ReadInt();

        Enabled = reader.ReadBool();
        QuestContentGenerated = reader.ReadBool();

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

        count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var chain = (FellowshipChain)reader.ReadInt();

            if (m != null)
            {
                _chains[m] = chain;
            }
        }

        count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var amount = reader.ReadInt();

            if (m != null && amount > 0)
            {
                _donations[m] = amount;
            }
        }
    }
}

public class FellowshipPersistence : GenericPersistence
{
    public FellowshipPersistence() : base("Fellowship", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => FellowshipData.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => FellowshipData.Deserialize(reader);
}
