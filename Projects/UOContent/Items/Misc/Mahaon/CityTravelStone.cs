using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CityTravelStone : Item
{
    [Constructible]
    public CityTravelStone() : base(0xED4)
    {
        Movable = false;
        Name = "камень телепорта";
        Hue = 0x489;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("Слишком далеко, чтобы использовать камень телепорта.");
            return;
        }

        from.SendGump(new CityTravelGump(from));
    }
}
