using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Taellia : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    private DateTime _spoken;

    [Constructible]
    public Taellia() : base("мудрец") => _spoken = Core.Now;

    public override bool CanTeach => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "старейшина Таэллия";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        Race = Race.Elf;

        Hue = 0x8385;
        HairItemID = 0x2FCD;
        HairHue = 0x368;
    }

    public override void InitOutfit()
    {
        AddItem(
            new Boots
            {
                Hue = 0x74B
            }
        );
        AddItem(
            new FemaleElvenRobe
            {
                Hue = 0x44
            }
        );
        AddItem(new Circlet());
        AddItem(new Item(0xDF2));
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (m.Alive && m is PlayerMobile && InRange(m, 5) && !InRange(oldLocation, 5) &&
            Core.Now >= _spoken + TimeSpan.FromMinutes(1))
        {
            // Welcome Seeker. Do you wish to embrace your elven heritage, casting aside your humanity,
            // and accepting the responsibilities of a caretaker of our beloved Sosaria. Then seek out
            // Darius the Wise in Moonglow. He will place you on the path.
            Say(1072800);
            _spoken = Core.Now;
        }
    }
}
