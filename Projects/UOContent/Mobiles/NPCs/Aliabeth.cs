using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Aliabeth : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Aliabeth() : base("жестянщица")
    {
        SetSkill(SkillName.Lockpicking, 60.0, 83.0);
        SetSkill(SkillName.RemoveTrap, 75.0, 98.0);
        SetSkill(SkillName.Tinkering, 64.0, 100.0);
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Алиабет";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBTinker());
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        Race = Race.Human;

        Hue = Race.RandomSkinHue();

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this);
    }

    public override void InitOutfit()
    {
        AddItem(new Backpack());
        AddItem(new Kilt(Utility.RandomNeutralHue()));
        AddItem(new Shirt(Utility.RandomNeutralHue()));
        AddItem(new Sandals());
    }
}
