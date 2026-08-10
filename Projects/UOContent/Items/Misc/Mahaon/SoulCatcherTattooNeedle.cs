using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SoulCatcherTattooNeedle : Item
{
    [Constructible]
    public SoulCatcherTattooNeedle() : base(0x0F9F)
    {
        Weight = 1.0;
        Name = "игла тату ловца душ";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (Systems.MahaonSoulStones.TattooSystem.HasActiveTattoo(from, Systems.MahaonSoulStones.TattooType.SoulCatcher))
        {
            from.SendMessage("У тебя уже есть свежее тату ловца душ.");
            return;
        }

        Systems.MahaonSoulStones.TattooSystem.ApplyTattoo(from, Systems.MahaonSoulStones.TattooType.SoulCatcher);
        Delete();
    }
}
