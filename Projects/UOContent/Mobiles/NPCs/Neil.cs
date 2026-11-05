using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Neil : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Neil() : base("кузнец")
    {
        SetSkill(SkillName.Blacksmith, 65.0, 88.0);
        SetSkill(SkillName.Fencing, 45.0, 68.0);
        SetSkill(SkillName.Macing, 45.0, 68.0);
        SetSkill(SkillName.Swords, 45.0, 68.0);
        SetSkill(SkillName.Tactics, 36.0, 68.0);
        SetSkill(SkillName.Parry, 61.0, 93.0);
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Нил";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBBlacksmith());
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = false;
        CantWalk = true;
        Race = Race.Human;

        Hue = 0x83F5;
        HairItemID = 0x203C;
        HairHue = 0x46F;
        FacialHairItemID = 0x203F;
        FacialHairHue = 0x46F;
    }

    public override void InitOutfit()
    {
        AddItem(new SmithHammer());
        AddItem(
            new ShortPants
            {
                Hue = 0x3A
            }
        );
        AddItem(
            new Bandana
            {
                Hue = 0x30
            }
        );
        AddItem(
            new Doublet
            {
                Hue = 0x13
            }
        );
        AddItem(new RingmailChest());
    }
}
