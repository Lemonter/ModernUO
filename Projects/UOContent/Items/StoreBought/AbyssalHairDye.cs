using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AbyssalHairDye : Item
{
    [Constructible]
    public AbyssalHairDye() : base(0xEFE)
    {
        Hue = 2075;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1149822; // Abyssal Hair Dye

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            return;
        }

        m.SendGump(new HairDyeConfirmGump(this, Hue));
    }
}
