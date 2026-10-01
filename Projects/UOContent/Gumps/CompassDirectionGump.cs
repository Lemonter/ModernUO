using Server.Regions;

namespace Server.Gumps;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/CompassDirectionGump.cs) — a tiny radar showing which of the four adjacent tiles is
// still on the safe path through the Maze of Death's trap corridor.
public class CompassDirectionGump : Gump
{
    public CompassDirectionGump(Mobile from) : base(100, 100)
    {
        var pointList = MazeOfDeathRegion.Path;

        var cur = new Point2D(from.Location.X, from.Location.Y);
        var northLoc = new Point2D(cur.X, cur.Y - 1);
        var eastLoc = new Point2D(cur.X + 1, cur.Y);
        var southLoc = new Point2D(cur.X, cur.Y + 1);
        var westLoc = new Point2D(cur.X - 1, cur.Y);

        AddImage(0, 0, 9007);
        AddAlphaRegion(0, 0, 200, 200);

        if (pointList.Contains(northLoc))
        {
            AddImage(100, 50, 4501);
        }

        if (pointList.Contains(eastLoc))
        {
            AddImage(100, 100, 4503);
        }

        if (pointList.Contains(southLoc))
        {
            AddImage(50, 100, 4505);
        }

        if (pointList.Contains(westLoc))
        {
            AddImage(50, 50, 4507);
        }
    }
}
