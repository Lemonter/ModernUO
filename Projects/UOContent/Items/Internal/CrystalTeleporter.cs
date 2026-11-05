using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CrystalTeleporter : ClickTeleporter
{
    [Constructible]
    public CrystalTeleporter() : this(0x1F19, new Point3D(0, 0, 0), null)
    {
    }

    [Constructible]
    public CrystalTeleporter(int itemID, Point3D pointDest, Map mapDest) : base(itemID, pointDest, mapDest)
    {
    }

    public override int LabelNumber => 1027961; // magical crystal
}
