using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Mobiles/Owain.cs).
///     ServUO's <c>StoryGump</c>/<c>BaseGump</c> story tree does not exist here, so his page is read out as the
///     first localized entry instead of opening a branching gump. <c>SetWearable</c> becomes
///     <c>AddItem(new X { Hue = hue })</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Owain : BaseVendor
{
    public static Point3D SpawnLocation { get; } = new(86, 223, -1);

    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Owain() : base("слепой")
    {
        Instance = this;
    }

    public static Owain Instance { get; set; }

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
            m.SendLocalizedMessage(1155664);
        }
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = "Оуайн";

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new Robe { Hue = 1255 });
        AddItem(new ThighBoots { Hue = 1908 });
    }
}

