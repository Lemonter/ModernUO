using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Alethanian : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Alethanian() : base("мудрец")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Алетаниан";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        Race = Race.Elf;

        Hue = 0x876C;
        HairItemID = 0x2FC2;
        HairHue = 0x368;
    }

    public override void InitOutfit()
    {
        AddItem(new ElvenBoots());
        AddItem(new GemmedCirclet());
        AddItem(new HidePants());
        AddItem(new HideFemaleChest());
        AddItem(new HidePauldrons());
    }
}
