using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Sorcerers/SorcererLegs.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SorcererLegs : LeatherLegs
{
    public override int LabelNumber => 1080489; // Sorcerer's Leggings

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public SorcererLegs() : base()
    {
        this.Hue = 1165;
        this.Weight = 4;

        this.Attributes.BonusInt = 1;
        this.Attributes.LowerRegCost = 10;

    }
}
