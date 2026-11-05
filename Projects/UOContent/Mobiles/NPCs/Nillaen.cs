using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Nillaen : BaseCreature
{
    [Constructible]
    public Nillaen() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "хранитель традиций";
        Race = Race.Elf;
        Female = true;
        Body = 0x25E;
        Hue = 0x8367;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        SetSkill(SkillName.Meditation, 60.0, 83.0);
        SetSkill(SkillName.Focus, 60.0, 83.0);

        HairItemID = 0x2FCF;
        HairHue = 0x26B;

        AddItem(
            new Shoes
            {
                Hue = 0x1BB
            }
        );
        AddItem(
            new LongPants
            {
                Hue = 0x1FB
            }
        );
        AddItem(new ElvenShirt());
        AddItem(new GemmedCirclet());
        AddItem(
            new BodySash
            {
                Hue = 0x25
            }
        );
        AddItem(new BlackStaff());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "хранитель традиций Ниллаэн";
    public override bool CanTeach => true;
}
