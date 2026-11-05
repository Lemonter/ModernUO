using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.CleanUpBritannia;

[SerializationGenerator(0, false)]
public partial class TheCleanupOfficer : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public TheCleanupOfficer() : base("офицер чистоты")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;

    public override bool IsInvulnerable => true;

    public override bool DisallowAllMoves => true;

    public override bool ClickTitle => true;

    public override bool CanTeach => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");

        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
        HairItemID = 0x2044;
        HairHue = 1644;
        FacialHairItemID = 0x203F;
        FacialHairHue = 1644;
    }

    public override void InitOutfit()
    {
        AddItem(new Cloak { Hue = 337 });
        AddItem(new ThighBoots());
        AddItem(new LongPants { Hue = 1409 });
        AddItem(new Doublet { Hue = 50 });
        AddItem(new FancyShirt { Hue = 1644 });
        AddItem(new Necklace());

        if (Backpack == null)
        {
            AddItem(new Backpack { Movable = false });
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1151317); // Clean Up Britannia Reward Trader
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 5))
        {
            pm.SendGump(new CleanUpBritanniaRewardGump(this, pm));
        }
    }
}
