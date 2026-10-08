using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.Despise;

/// <summary>
///     Replaces ServUO's DespiseCrystals (Scripts/Services/PointsSystems/DespiseCrystals.cs)
///     — that was a subclass of Engines.Points.PointsSystem, a generic reward-currency
///     framework (used for several ServUO systems, faction-points-style) that has no
///     equivalent anywhere in this codebase. Rather than port the whole framework for this
///     one reward currency, this is a small standalone tracker with just what PutridHeart
///     actually needs: award points, read a total, persist across restarts.
/// </summary>
public static class DespiseCrystals
{
    private static readonly Dictionary<PlayerMobile, double> _points = new();

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static double GetPoints(PlayerMobile pm) => _points.GetValueOrDefault(pm, 0.0);

    public static void AwardPoints(Mobile from, double amount)
    {
        if (from is not PlayerMobile pm || amount <= 0)
        {
            return;
        }

        _points[pm] = GetPoints(pm) + amount;
        pm.SendLocalizedMessage(1153423, ((int)amount).ToString()); // You have gained ~1_AMT~ Dungeon Crystal Points of Despise.
    }

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("DespiseCrystals", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(_points.Count);

            foreach (var (pm, points) in _points)
            {
                writer.Write(pm);
                writer.Write(points);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            var count = reader.ReadEncodedInt();

            for (var i = 0; i < count; i++)
            {
                var pm = reader.ReadEntity<PlayerMobile>();
                var points = reader.ReadDouble();

                if (pm != null)
                {
                    _points[pm] = points;
                }
            }
        }
    }
}
