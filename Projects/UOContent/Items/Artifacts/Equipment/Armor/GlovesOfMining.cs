using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/GlovesOfMining.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishKiltOfMining : BaseGlovesOfMining
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishKiltOfMining() : this(5)
    {
    }

    public GargishKiltOfMining(int bonus)
        : base(bonus, 0x030C)
    {
        Weight = 1;
    }

    public override int BasePhysicalResistance => 3;

    public override int BaseFireResistance => 3;

    public override int BaseColdResistance => 1;

    public override int BasePoisonResistance => 5;

    public override int BaseEnergyResistance => 3;

    public override int InitMinHits => 40;

    public override int InitMaxHits => 50;

    public override int AosStrReq => 40;

    public override int OldStrReq => 20;

    public override int OldDexBonus => -1;

    public override int ArmorBase => 22;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Ringmail;

    public override int LabelNumber => 1045124; // ringmail blacksmith gloves of mining
}
