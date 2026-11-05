using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Acob : BaseCreature
{
    [Constructible]
    public Acob() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "мудрец";
        Race = Race.Elf;
        Female = false;
        Body = 0x25D;
        Hue = 0x8389;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        SetSkill(SkillName.Meditation, 60.0, 83.0);
        SetSkill(SkillName.Focus, 60.0, 83.0);

        HairItemID = 0x2FCF;
        HairHue = 0x389;

        AddItem(new Backpack());
        AddItem(
            new ElvenBoots
            {
                Hue = 0x73D
            }
        );
        AddItem(new HidePants());
        AddItem(
            new ElvenShirt
            {
                Hue = 0x71
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Акоб";
    public override bool CanTeach => true;
}
