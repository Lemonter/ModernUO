using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives this NPC from MondainQuester, which does not exist in ModernUO. The SBCarpets shop
///     info and the PlayerMobile.CanBuyCarpets gate it relies on are missing, so InitSBInfo is empty and the
///     carpet sale restriction was dropped. The quest dialogue and quest list are not ported either.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Laifem : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Laifem() : base("ткачиха")
    {
    }

    public override bool IsInvulnerable => true;
    public override string DefaultName => "Лаифем";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        CantWalk = true;

        Race = Race.Gargoyle;
        HairItemID = Race.RandomHair(true);
        Hue = Race.RandomSkinHue();
        HairHue = Race.RandomHairHue();
    }

    public override void InitOutfit()
    {
        AddItem(new Backpack());
        AddItem(new GargishClothChestType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue() });
        AddItem(new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue() });
    }
}
