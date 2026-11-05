using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue, the Advertise/OnOfferFailed quest hooks and the quest list.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Amelia : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Amelia() : base("инструктор по жестянщику")
    {
        SetSkill(SkillName.ArmsLore, 120.0, 120.0);
        SetSkill(SkillName.Blacksmith, 120.0, 120.0);
        SetSkill(SkillName.Magery, 120.0, 120.0);
        SetSkill(SkillName.Tactics, 120.0, 120.0);
        SetSkill(SkillName.Swords, 120.0, 120.0);
        SetSkill(SkillName.Tinkering, 120.0, 120.0);
        SetSkill(SkillName.Mining, 120.0, 120.0);
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Амелия Янгстоун";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBTinker());
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        CantWalk = true;
        Race = Race.Human;

        Hue = Race.RandomSkinHue();

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this);
    }

    public override void InitOutfit()
    {
        AddItem(new Backpack());
        AddItem(new Sandals());
        AddItem(new ShortPants());
        AddItem(
            new HalfApron
            {
                Hue = 0x8AB
            }
        );
        AddItem(new Doublet());
    }
}
