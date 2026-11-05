using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Athialon : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Athialon() : base("участник экспедиции")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "Атиалон";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = false;
        Race = Race.Elf;

        Hue = 0x8382;
        HairItemID = 0x2FC0;
        HairHue = 0x35;
    }

    public override void InitOutfit()
    {
        AddItem(
            new ElvenBoots
            {
                Hue = 0x901
            }
        );
        AddItem(new DiamondMace());
        AddItem(new WoodlandBelt());
        AddItem(
            new WoodlandLegs
            {
                Hue = 0x3B2
            }
        );
        AddItem(
            new WoodlandChest
            {
                Hue = 0x3B2
            }
        );
        AddItem(
            new WoodlandArms
            {
                Hue = 0x3B2
            }
        );
        AddItem(
            new WingedHelm
            {
                Hue = 0x3B2
            }
        );
    }
}
