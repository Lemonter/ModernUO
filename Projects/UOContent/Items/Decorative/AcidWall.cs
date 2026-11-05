using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AcidWall : BaseWall
{
    [Constructible]
    public AcidWall() : base(969)
    {
        Hue = 1828;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(1111659); // You try to examine the strange wall but the vines get in your way.
        }
        else
        {
            from.SendLocalizedMessage(1019045); // I can't reach that.
        }
    }
}
