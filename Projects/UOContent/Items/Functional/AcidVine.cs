using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AcidVine : Item
{
    [Constructible]
    public AcidVine() : base(3313)
    {
        Weight = 1.0;
        Movable = false;
    }

    public override int LabelNumber => 1111655; // magic vines

    public override bool ForceShowProperties => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 1))
        {
            // The vines tighten their grip, stopping you from opening the door.
            from.SendLocalizedMessage(1111658);
        }
        else if (from.InRange(GetWorldLocation(), 4))
        {
            // You notice something odd about the vines covering the wall.
            from.SendLocalizedMessage(1111665);
        }
        else
        {
            from.SendLocalizedMessage(1019045); // I can't reach that.
        }

        base.OnDoubleClick(from);
    }
}
