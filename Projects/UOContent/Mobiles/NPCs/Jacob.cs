using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue, the Advertise/OnOfferFailed quest hooks and the quest list.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Jacob : BaseCreature
{
    [Constructible]
    public Jacob() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "инструктор по горному делу";
        Race = Race.Human;
        Female = false;
        Body = 0x190;
        Hue = 0x83E8;

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        SetSkill(SkillName.ArmsLore, 120.0, 120.0);
        SetSkill(SkillName.Blacksmith, 120.0, 120.0);
        SetSkill(SkillName.Magery, 120.0, 120.0);
        SetSkill(SkillName.Tactics, 120.0, 120.0);
        SetSkill(SkillName.Tinkering, 120.0, 120.0);
        SetSkill(SkillName.Swords, 120.0, 120.0);
        SetSkill(SkillName.Mining, 120.0, 120.0);

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this);

        AddItem(new Backpack());
        AddItem(new Pickaxe());
        AddItem(new Boots());
        AddItem(
            new ShortPants
            {
                Hue = 0x370
            }
        );
        AddItem(
            new Shirt
            {
                Hue = 0x966
            }
        );
        AddItem(
            new WideBrimHat
            {
                Hue = 0x966
            }
        );
        AddItem(
            new HalfApron
            {
                Hue = 0x1BB
            }
        );
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Джейкоб Уолтцт";
    public override bool CanTeach => true;
}
