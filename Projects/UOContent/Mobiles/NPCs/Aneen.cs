using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Aneen : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Aneen() : base("хранитель традиций")
    {
    }

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "хранитель традиций Анин";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = false;
        Race = Race.Elf;

        Hue = 0x83E5;
        HairItemID = 0x2FBF;
        HairHue = 0x90;
    }

    public override void InitOutfit()
    {
        AddItem(
            new Sandals
            {
                Hue = 0x1BB
            }
        );
        AddItem(
            new MaleElvenRobe
            {
                Hue = 0x48F
            }
        );
        AddItem(new Item(0xDF2));
    }
}
