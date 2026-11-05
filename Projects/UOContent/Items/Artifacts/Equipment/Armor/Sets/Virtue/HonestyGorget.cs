using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtue/HonestyGorget.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HonestyGorget : BaseArmor
{
    [Constructible]
    public HonestyGorget()
        : base(0x2B0E)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 2.0;
        this.Hue = 0x226;

    }

    public override int LabelNumber => 1075189; // Gorget of Honesty (Virtue Armor Set)

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 9;

    public override int BasePoisonResistance => 5;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int AosStrReq => 45;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
}
