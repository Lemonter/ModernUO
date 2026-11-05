using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. The alchemy bulk order
///     support and the inspected ingot/keg turn-in (SmallAlchemyBOD, LargeAlchemyBOD, PileofInspected*,
///     InspectedKegof*) are missing here, so those overrides were dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Zosilem : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Zosilem() : base("алхимик")
    {
        SetSkill(SkillName.Alchemy, 85.0, 100.0);
        SetSkill(SkillName.TasteID, 65.0, 88.0);
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Зосилем";

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

        Hue = 0x86EA;
        HairItemID = 0x4273;
        HairHue = 0x323;
    }

    public override void InitOutfit()
    {
        AddItem(
            new GargishClothLegsType2
            {
                Hue = 0x736
            }
        );
        AddItem(
            new GargishClothKiltType2
            {
                Hue = 0x73D
            }
        );
        AddItem(
            new GargishClothChestType2
            {
                Hue = 0x38B
            }
        );
        AddItem(
            new GargishClothArmsType2
            {
                Hue = 0x711
            }
        );
    }
}
