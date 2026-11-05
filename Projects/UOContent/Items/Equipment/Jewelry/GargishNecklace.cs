using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/GargishNecklace.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishNecklace : BaseArmor
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;
    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override int BasePhysicalResistance => 1;
    public override int BaseFireResistance => 2;
    public override int BaseColdResistance => 2;
    public override int BasePoisonResistance => 2;
    public override int BaseEnergyResistance => 3;

    public override int InitMinHits => 30;
    public override int InitMaxHits => 40;

    [Constructible]
    public GargishNecklace()
        : base(0x4210)
    {
        Layer = Layer.Neck;
    }

    public GargishNecklace(int itemID)
        : base(itemID)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GargishAmulet : GargishNecklace
{
    [Constructible]
    public GargishAmulet()
        : base(0x4D0B)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GargishStoneAmulet : GargishNecklace
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Stone;
    public override int AosStrReq => 40;
    public override int OldStrReq => 20;

    [Constructible]
    public GargishStoneAmulet()
        : base(0x4D0A)
    {
        this.Hue = 2500;
    }
}
