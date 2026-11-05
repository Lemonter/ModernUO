using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest chain classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Broolol : BaseCreature
{
    [Constructible]
    public Broolol() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "хранитель традиций";
        Race = Race.Elf;
        Female = false;
        Body = 0x25D;
        Hue = 0x851D;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        SetSkill(SkillName.Meditation, 60.0, 83.0);
        SetSkill(SkillName.Focus, 60.0, 83.0);

        HairItemID = 0x2FCF;
        HairHue = 0x323;

        AddItem(
            new ElvenBoots
            {
                Hue = 0x71B
            }
        );
        AddItem(
            new MaleElvenRobe
            {
                Hue = 0x1C
            }
        );
        AddItem(new WildStaff());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "хранитель традиций Броолол";
    public override bool CanTeach => true;
}
