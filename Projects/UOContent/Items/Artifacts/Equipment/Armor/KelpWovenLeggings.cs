using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/KelpWovenLeggings.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KelpWovenLeggings : LeatherLegs
{
    public override int LabelNumber => 1149960;

    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 13;

    public override int BaseColdResistance => 12;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 14;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public KelpWovenLeggings()
    {
        Hue = 1155;
        Attributes.BonusHits = 5;
        Attributes.BonusMana = 8;
        Attributes.RegenMana = 2;
        Attributes.SpellDamage = 8;
        Attributes.LowerRegCost = 15;
    }
}
