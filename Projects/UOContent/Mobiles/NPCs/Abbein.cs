using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Abbein : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Abbein() : base("мудрец")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Аббейн";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        Race = Race.Elf;

        Hue = 0x824D;
        HairItemID = 0x2FD1;
        HairHue = 0x321;
    }

    public override void InitOutfit()
    {
        AddItem(
            new ElvenBoots
            {
                Hue = 0x74B
            }
        );
        AddItem(
            new FemaleElvenRobe
            {
                Hue = 0x8A8
            }
        );
        AddItem(new RoyalCirclet());
    }
}
