using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Oolua : BaseCreature
{
    [Constructible]
    public Oolua() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "хранитель традиций";
        Race = Race.Elf;
        Female = true;
        Body = 0x25E;
        Hue = 0x853F;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        HairItemID = 0x2FCC;
        HairHue = 0x388;

        AddItem(
            new ElvenBoots
            {
                Hue = 0x70E
            }
        );
        AddItem(new WildStaff());
        AddItem(new GemmedCirclet());
        AddItem(
            new Cloak
            {
                Hue = 0x1BB
            }
        );
        AddItem(
            new Skirt
            {
                Hue = 0x3
            }
        );
        AddItem(
            new FancyShirt
            {
                Hue = 0x70A
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "хранитель традиций Оолуа";
    public override bool CanTeach => true;
}
