using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Mobiles/Elizabeth.cs).
///     ServUO's <c>StoryGump</c>/<c>BaseGump</c> story tree does not exist here, so her page is read out as the
///     first localized entry instead of opening a branching gump. <c>SetWearable</c> becomes
///     <c>AddItem(new X { Hue = hue })</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Elizabeth : BaseVendor
{
    public static Point3D SpawnLocation { get; } = new(2364, 1284, -90);

    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Elizabeth() : base("профессор")
    {
        Instance = this;
    }

    public static Elizabeth Instance { get; set; }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;

    public override bool IsInvulnerable => true;

    public override bool DisallowAllMoves => true;

    public override bool ClickTitle => true;

    public override bool CanTeach => false;

    public override void InitSBInfo()
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m.InRange(Location, 5))
        {
            m.SendLocalizedMessage(1155648);
        }
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = "Элизабет";
        Female = true;

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x191;
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt { Hue = 1255 });
        AddItem(new JinBaori { Hue = 2722 });
        AddItem(new Kilt { Hue = 2012 });
        AddItem(new ThighBoots { Hue = 1908 });
    }
}

