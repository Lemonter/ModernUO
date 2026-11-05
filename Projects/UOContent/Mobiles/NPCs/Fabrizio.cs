using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Fabrizio : BaseCreature
{
    [Constructible]
    public Fabrizio() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "мастер-оружейник";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x840E;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        HairItemID = 0x203D;
        HairHue = 0x1;
        FacialHairItemID = 0x203F;
        FacialHairHue = 0x1;

        AddItem(new Backpack());
        AddItem(
            new Shoes
            {
                Hue = 0x753
            }
        );
        AddItem(
            new LongPants
            {
                Hue = 0x59C
            }
        );
        AddItem(
            new HalfApron
            {
                Hue = 0x8FD
            }
        );
        AddItem(
            new Tunic
            {
                Hue = 0x58F
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Фабрицио";
    public override bool CanTeach => true;
}
