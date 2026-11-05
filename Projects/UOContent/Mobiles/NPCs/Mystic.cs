using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO's SBMystic shop info class does not exist in ModernUO, so InitSBInfo is left empty and the
///     mystic does not sell anything.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Mystic : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Mystic() : base("мистик")
    {
        SetSkill(SkillName.EvalInt, 65.0, 88.0);
        SetSkill(SkillName.Inscribe, 60.0, 83.0);
        SetSkill(SkillName.Mysticism, 64.0, 100.0);
        SetSkill(SkillName.Meditation, 60.0, 83.0);
        SetSkill(SkillName.MagicResist, 65.0, 88.0);
        SetSkill(SkillName.Wrestling, 36.0, 68.0);
    }

    public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals;

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(
            new Robe
            {
                Hue = Utility.RandomBlueHue()
            }
        );
    }
}
