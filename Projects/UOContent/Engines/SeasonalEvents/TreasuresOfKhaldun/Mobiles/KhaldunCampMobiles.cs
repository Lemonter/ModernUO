using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Regions;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/KhaldunCampRegion.cs).
///     <c>SetWearable</c> becomes <c>AddItem(new X { Hue = hue })</c>; the vendor has no stock, it only staffs the
///     camp gate.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunCampGuard : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public KhaldunCampGuard() : base("стражник")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = Utility.RandomBool();
        CantWalk = true;

        Name = NameList.RandomName(Female ? "female" : "male");

        Race = Race.Human;
        Hue = Race.RandomSkinHue();
        HairHue = Race.RandomHairHue();
        FacialHairHue = Race.RandomHairHue();
        HairItemID = Race.RandomHair(Female);
        FacialHairItemID = Race.RandomFacialHair(Female);
    }

    public override void InitOutfit()
    {
        AddItem(new PlateChest());
        AddItem(new PlateLegs());
        AddItem(new PlateGorget());
        AddItem(new PlateGloves());
        AddItem(new Halberd());
        AddItem(new PlateArms());
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/KhaldunCampRegion.cs).
///     ServUO's <c>BasicInfoGump</c> does not exist here, so the investigator speaks the same cliloc through a
///     localized message instead of a scrollable window. <c>SetWearable</c> becomes
///     <c>AddItem(new X { Hue = hue })</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class LeadInvestigator : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public LeadInvestigator() : base("старший следователь")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = Utility.RandomBool();
        CantWalk = true;

        Name = NameList.RandomName(Female ? "female" : "male");

        Race = Race.Human;
        Hue = Race.RandomSkinHue();
        HairHue = Race.RandomHairHue();
        FacialHairHue = Race.RandomHairHue();
        HairItemID = Race.RandomHair(Female);
        FacialHairItemID = Race.RandomFacialHair(Female);
    }

    public override void InitOutfit()
    {
        AddItem(new Shirt { Hue = 1354 });
        AddItem(new Doublet { Hue = 1504 });
        AddItem(new Kilt { Hue = 1351 });
        AddItem(new LongPants { Hue = 1743 });
        AddItem(new GoldRing());
        AddItem(new GoldBracelet());
        AddItem(new Shoes());
        AddItem(new GoldNecklace());
        AddItem(new GoldEarrings());
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!InRange(from.Location, 3) || from.FindItemOnLayer(Layer.Neck) is not DetectiveCredentials)
        {
            return;
        }

        Direction = Utility.GetDirection(Location, from.Location);

        // ServUO opened BasicInfoGump(1158700) here; this codebase has no BasicInfoGump.
        from.SendLocalizedMessage(1158700);
    }
}
