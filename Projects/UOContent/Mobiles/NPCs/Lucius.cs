using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Lucius : BaseCreature
{
    [Constructible]
    public Lucius() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "искатель приключений";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x83F3;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        HairItemID = 0x2047;
        HairHue = 0x393;
        FacialHairItemID = 0x203F;
        FacialHairHue = 0x393;

        AddItem(new Backpack());
        AddItem(
            new Boots
            {
                Hue = 0x717
            }
        );
        AddItem(
            new LongPants
            {
                Hue = 0x1BB
            }
        );
        AddItem(
            new Cloak
            {
                Hue = 0x71
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Люциус";
    public override bool CanTeach => true;
}
