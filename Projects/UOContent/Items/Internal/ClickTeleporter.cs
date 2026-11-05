using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ClickTeleporter : Teleporter
{
    [Constructible]
    public ClickTeleporter(int itemID = 0x1BC3) : this(itemID, new Point3D(0, 0, 0), null)
    {
    }

    public ClickTeleporter(int itemID, Point3D pointDest, Map mapDest) : base(pointDest, mapDest)
    {
        ItemID = itemID;
        Movable = false;
        Visible = true;
        Weight = 0;

        SetFlag(TeleporterFlags.Active, true);
        PointDest = pointDest;
        MapDest = mapDest;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 3) || !from.InLOS(this) || !from.CanSee(this))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else
        {
            OnMoveOver(from);
        }
    }
}
