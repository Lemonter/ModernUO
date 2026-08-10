using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Systems.MahaonProfessions;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ProfessionStone : Item
{
    [Constructible]
    public ProfessionStone() : base(0xED4)
    {
        Movable = false;
        Name = "камень профессий";
        Hue = 0x47E;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("Слишком далеко, чтобы использовать камень профессий.");
            return;
        }

        if (from is not PlayerMobile pm)
        {
            return;
        }

        from.SendGump(new Systems.MahaonProfessions.MahaonProfessionPickerGump(pm));
    }
}
