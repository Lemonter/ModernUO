using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO's SBNecromancer shop info class does not exist in ModernUO, so InitSBInfo is left empty and the
///     necromancer does not sell anything.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Necromancer : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Necromancer() : base("некромант")
    {
        SetSkill(SkillName.EvalInt, 80.0, 100.0);
        SetSkill(SkillName.Inscribe, 80.0, 100.0);
        SetSkill(SkillName.Necromancy, 80.0, 100.0);
        SetSkill(SkillName.Meditation, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 80.0, 100.0);

        Hue = 0x3C6;
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(
            new Shoes
            {
                Hue = 0x151
            }
        );
        AddItem(
            new Robe
            {
                Hue = 0x455
            }
        );
        AddItem(
            new FancyShirt
            {
                Hue = 0x455
            }
        );

        var hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A))
        {
            Hue = 0x3C6,
            Layer = Layer.Hair,
            Movable = false
        };
        AddItem(hair);

        var beard = new Item(0x0)
        {
            Layer = Layer.FacialHair
        };
        AddItem(beard);
    }
}
