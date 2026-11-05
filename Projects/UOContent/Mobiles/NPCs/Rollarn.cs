using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Rollarn : BaseCreature
{
    [Constructible]
    public Rollarn() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "хранитель традиций";

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        Female = false;
        CantWalk = true;
        Race = Race.Elf;

        Hue = 0x84DE;
        HairItemID = 0x2FC1;
        HairHue = 0x320;

        AddItem(
            new Shoes
            {
                Hue = 0x1BB
            }
        );
        AddItem(new Circlet());
        AddItem(
            new Cloak
            {
                Hue = 0x296
            }
        );
        AddItem(new LeafChest());
        AddItem(new LeafArms());
        AddItem(
            new LeafLegs
            {
                Hue = 0x74E
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "хранитель традиций Ролларн";
    public override bool CanTeach => true;
}
