using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO's alchemy bulk order support (SmallAlchemyBOD, LargeAlchemyBOD, the inspected-ingot piles and
///     the BODType overrides) does not exist in ModernUO, so it was dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Thepem : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Thepem() : base("подмастерье")
    {
        SetSkill(SkillName.Alchemy, 85.0, 100.0);
        SetSkill(SkillName.TasteID, 65.0, 88.0);
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Тепем";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBAlchemist());
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        Race = Race.Gargoyle;

        Hue = 0x86E1;
        HairItemID = 0x42AB;
        HairHue = 0x385;
    }

    public override void InitOutfit()
    {
        AddItem(
            new GargishClothLegsType2
            {
                Hue = 0x70F
            }
        );
        AddItem(
            new GargishClothKiltType2
            {
                Hue = 0x742
            }
        );
        AddItem(
            new GargishClothChestType2
            {
                Hue = 0x4C3
            }
        );
        AddItem(
            new GargishClothArmsType2
            {
                Hue = 0x738
            }
        );
    }
}
