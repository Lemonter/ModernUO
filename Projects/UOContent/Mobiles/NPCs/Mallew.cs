using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Mallew : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Mallew() : base("мудрец")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Маллью";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = false;
        Race = Race.Elf;

        Hue = 0x876C;
        HairItemID = 0x2FD1;
        HairHue = 0x31E;
    }

    public override void InitOutfit()
    {
        AddItem(
            new ElvenBoots
            {
                Hue = 0x1BB
            }
        );
        AddItem(new Circlet());
        AddItem(
            new Cloak
            {
                Hue = 0x3B2
            }
        );
        AddItem(
            new LeafChest
            {
                Hue = 0x53E
            }
        );
        AddItem(
            new LeafArms
            {
                Hue = 0x53E
            }
        );
        AddItem(
            new LeafTonlet
            {
                Hue = 0x53E
            }
        );
    }
}
