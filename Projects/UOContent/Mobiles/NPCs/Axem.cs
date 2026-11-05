using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Axem : BaseCreature
{
    [Constructible]
    public Axem() : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Title = "куратор";

        SetSpeed(0.5, 2.0);
        InitStats(100, 100, 25);

        SetSkill(SkillName.Meditation, 60.0, 83.0);
        SetSkill(SkillName.Focus, 60.0, 83.0);

        Female = false;
        CantWalk = true;
        Body = 666;
        HairItemID = 16987;
        HairHue = 1801;

        AddItem(new Backpack());
        AddItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Аксем";
    public override bool CanTeach => true;
}
