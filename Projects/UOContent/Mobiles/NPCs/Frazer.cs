using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. Ported without the
///     quest dialogue and quest list, which depend on the missing ML quest classes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Frazer : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Frazer() : base("бродяга") => SetSkill(SkillName.ItemID, 64.0, 100.0);

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Фрейзер";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBJewel());
        _sbInfos.Add(new SBTinker());
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = false;
        Race = Race.Human;

        Hue = 0x840F;
        HairItemID = 0x204A;
        HairHue = 0x45A;
        FacialHairItemID = 0x204D;
        FacialHairHue = 0x45A;
    }

    public override void InitOutfit()
    {
        AddItem(
            new Shoes
            {
                Hue = 0x735
            }
        );
        AddItem(
            new LongPants
            {
                Hue = 0x4C0
            }
        );
        AddItem(
            new FancyShirt
            {
                Hue = 0x3
            }
        );
        AddItem(
            new JesterHat
            {
                Hue = 0x74A
            }
        );
    }
}
