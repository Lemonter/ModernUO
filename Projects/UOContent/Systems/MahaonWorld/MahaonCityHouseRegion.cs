using System;
using System.Collections.Generic;
using Server.Items;
using Server.Regions;

namespace Server.Systems.MahaonWorld;

/// <summary>
/// The region of one city apartment: its marked floor, one area per row of tiles, a few Z above
/// and below each. Gives the apartment what a house region gives a house — banned people kept
/// out, an instant logout for those who live there, no house placement — while the town it sits
/// in stays its parent, so guards and town rules still apply inside.
/// </summary>
public class MahaonCityHouseRegion : BaseRegion
{
    private const int FloorBelow = 3;
    private const int Headroom = 20;

    public MahaonCityHouseRegion(MahaonCityHouse house)
        : base(null, house.AreaMap, Find(house.Tiles[0], house.AreaMap), DefaultPriority + 1, Areas(house)) => House = house;

    public MahaonCityHouse House { get; }

    /// <summary>Tiles merged into horizontal runs, one area each.</summary>
    private static Rectangle3D[] Areas(MahaonCityHouse house)
    {
        var rows = new SortedDictionary<(int y, int z), List<int>>();
        foreach (var t in house.Tiles)
        {
            if (!rows.TryGetValue((t.Y, t.Z), out var xs))
            {
                rows[(t.Y, t.Z)] = xs = [];
            }

            xs.Add(t.X);
        }

        var areas = new List<Rectangle3D>();
        foreach (var ((y, z), xs) in rows)
        {
            xs.Sort();
            var start = xs[0];
            var prev = start;

            for (var i = 1; i <= xs.Count; i++)
            {
                if (i < xs.Count && xs[i] == prev + 1)
                {
                    prev = xs[i];
                    continue;
                }

                areas.Add(new Rectangle3D(new Point3D(start, y, z - FloorBelow), new Point3D(prev + 1, y + 1, z + Headroom)));

                if (i < xs.Count)
                {
                    start = prev = xs[i];
                }
            }
        }

        return areas.ToArray();
    }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
    {
        if (House.IsBanned(m) && m.AccessLevel == AccessLevel.Player)
        {
            m.SendMessage(0x22, "Тебе сюда нельзя — хозяин тебя не пускает.");
            return false;
        }

        return base.OnMoveInto(m, d, newLocation, oldLocation);
    }

    public override TimeSpan GetLogoutDelay(Mobile m) => House.IsFriend(m) ? TimeSpan.Zero : base.GetLogoutDelay(m);
}
