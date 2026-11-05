using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TeleportRope : Static
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.Administrator)]
    private Point3D _toLocation;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.Administrator)]
    private Map _toMap;

    [Constructible]
    public TeleportRope() : base(0x14FA)
    {
        _toLocation = Point3D.Zero;
        _toMap = Map.Internal;
    }

    public virtual bool AllowDead => true;

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        if (_toLocation == Point3D.Zero || _toMap == Map.Internal)
        {
            return;
        }

        from.MoveToWorld(_toLocation, _toMap);
    }

    public override void OnDoubleClickDead(Mobile from)
    {
        if (AllowDead)
        {
            OnDoubleClick(from);
        }
        else
        {
            base.OnDoubleClickDead(from);
        }
    }
}
