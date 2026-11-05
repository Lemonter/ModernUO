using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Natalie : BaseCreature
{
    [Constructible]
    public Natalie() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "леди Скара Брэя";
        Race = Race.Human;
        Female = true;
        Body = 0x191;
        Hue = 0x8400;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        CantWalk = true;

        HairItemID = 0x2045;
        HairHue = 0x740;

        AddItem(new Backpack());
        AddItem(
            new Shoes
            {
                Hue = 0x727
            }
        );
        AddItem(
            new FancyShirt
            {
                Hue = 0x53C
            }
        );
        AddItem(
            new Skirt
            {
                Hue = 0x534
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Натали";
    public override bool CanTeach => true;
}
