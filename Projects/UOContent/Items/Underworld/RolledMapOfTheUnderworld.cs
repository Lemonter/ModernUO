using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/RolledMapOfTheUnderworld.cs) — a plain decorative map image, standing near the
// Maze of Death entrance.
[SerializationGenerator(0, false)]
public partial class RolledMapOfTheUnderworld : Item
{
    [Constructible]
    public RolledMapOfTheUnderworld() : base(5357)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 3))
        {
            from.CloseGump<UnderworldMapGump>();
            from.SendGump(new UnderworldMapGump());
        }
    }

    private class UnderworldMapGump : Gump
    {
        public UnderworldMapGump() : base(75, 75)
        {
            AddImage(0, 0, 0x7739);
        }
    }
}
