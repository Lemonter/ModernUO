using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Vicaie : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Vicaie() : base("мудрец")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Викай";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        Race = Race.Elf;

        Hue = 0x8362;
        HairItemID = 0x2FCD;
        HairHue = 0x90;
    }

    public override void InitOutfit()
    {
        AddItem(new ElvenBoots());
        AddItem(
            new Tunic
            {
                Hue = 0x1FA1
            }
        );
        AddItem(
            new LeafLegs
            {
                Hue = 0x3B3
            }
        );
    }
}
