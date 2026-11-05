using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Calendor : BaseCreature
{
    [Constructible]
    public Calendor() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "хранитель традиций";
        Race = Race.Elf;
        Female = true;
        Body = 0x25E;
        Hue = 0x847E;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        SetSkill(SkillName.Meditation, 60.0, 83.0);
        SetSkill(SkillName.Focus, 60.0, 83.0);

        HairItemID = 0x2FD0;
        HairHue = 0x1F2;

        AddItem(
            new ElvenBoots
            {
                Hue = 0x65A
            }
        );
        AddItem(
            new ElvenShirt
            {
                Hue = 0x728
            }
        );
        AddItem(
            new Kilt
            {
                Hue = 0x1BB
            }
        );
        AddItem(new RoyalCirclet());
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "хранитель традиций Календор";
    public override bool CanTeach => true;
}
