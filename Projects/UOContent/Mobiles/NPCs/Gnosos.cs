using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO, and exposes the
///     BaseVendor ConvertsMageArmor hook, which ModernUO does not have. Ported without the quest dialogue,
///     the quest list and the mage armor conversion hook.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Gnosos : BaseCreature
{
    [Constructible]
    public Gnosos() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "некромант";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x83E8;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        SetSkill(SkillName.Focus, 60.0, 83.0);
        SetSkill(SkillName.EvalInt, 65.0, 88.0);
        SetSkill(SkillName.Inscribe, 60.0, 83.0);
        SetSkill(SkillName.Necromancy, 64.0, 100.0);
        SetSkill(SkillName.Meditation, 60.0, 83.0);
        SetSkill(SkillName.MagicResist, 65.0, 88.0);
        SetSkill(SkillName.SpiritSpeak, 36.0, 68.0);

        HairItemID = 0x203B;
        FacialHairItemID = 0x2040;

        AddItem(new Backpack());
        AddItem(
            new Shoes
            {
                Hue = 0x485
            }
        );
        AddItem(
            new Robe
            {
                Hue = 0x497
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "мастер Гноссос";
    public override bool CanTeach => true;
}
