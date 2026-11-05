using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Jothan : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Jothan() : base("мудрец")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Джотан";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = false;
        Race = Race.Elf;

        Hue = 0x8579;
        HairItemID = 0x2FC2;
        HairHue = 0x2C2;
    }

    public override void InitOutfit()
    {
        AddItem(new ThighBoots());
        AddItem(
            new ElvenPants
            {
                Hue = 0x57A
            }
        );
        AddItem(
            new ElvenShirt
            {
                Hue = 0x711
            }
        );
        AddItem(
            new Cloak
            {
                Hue = 0x21
            }
        );
        AddItem(new Circlet());
    }
}
