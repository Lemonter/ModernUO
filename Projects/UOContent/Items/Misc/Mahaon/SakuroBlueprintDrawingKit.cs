using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SakuroBlueprintDrawingKit : Item
{
    [Constructible]
    public SakuroBlueprintDrawingKit() : base(0x0FBF)
    {
        Weight = 3.0;
        Name = "набор для черчения чертежей";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendGump(new Gumps.SakuroBlueprintDrawingGump(from));
    }
}
