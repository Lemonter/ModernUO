using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Tyleelor : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Tyleelor() : base("участник экспедиции")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "Тайлилор";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = false;
        Race = Race.Elf;

        Hue = 0x8367;
        HairItemID = 0x2FC1;
        HairHue = 0x38;
    }

    public override void InitOutfit()
    {
        AddItem(
            new ElvenBoots
            {
                Hue = 0x1BB
            }
        );
        AddItem(
            new WoodlandLegs
            {
                Hue = 0x236
            }
        );
        AddItem(
            new WoodlandChest
            {
                Hue = 0x236
            }
        );
        AddItem(
            new WoodlandArms
            {
                Hue = 0x236
            }
        );
        AddItem(
            new WoodlandBelt
            {
                Hue = 0x237
            }
        );
        AddItem(
            new VultureHelm
            {
                Hue = 0x236
            }
        );
    }
}
