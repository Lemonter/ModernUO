namespace Server.Engines.VvV;

// ServUO's gumps use `Quests.BaseQuestGump.C32216` (a 24-bit RGB -> 16-bit gump text color
// converter) which doesn't exist in this codebase — reimplemented here since several VvV
// gumps need it.
public static class GumpColor
{
    public static int Convert32To16(int rgb)
    {
        var r = (rgb >> 19) & 0x1F;
        var g = (rgb >> 11) & 0x1F;
        var b = (rgb >> 3) & 0x1F;

        return 0x8000 | (r << 10) | (g << 5) | b;
    }
}
