using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Lever : Item
{
    [Constructible]
    public Lever() : base(Utility.RandomList(0x108C, 0x108D, 0x108E, 0x1093, 0x1094, 0x1095, 0x108F, 0x1090, 0x1091, 0x1092))
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(500357 + Utility.Random(5));
    }
}
