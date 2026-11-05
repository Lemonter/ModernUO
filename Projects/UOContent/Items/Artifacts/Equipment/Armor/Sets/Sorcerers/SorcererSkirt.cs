using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Sorcerers/SorcererSkirt.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SorcererSkirt : LeatherSkirt
{
    public override int LabelNumber => 1080471; // Sorcerer's Skirt

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public SorcererSkirt() : base()
    {
        this.Hue = 1165;
        this.Weight = 1;

        this.Attributes.BonusInt = 1;
        this.Attributes.LowerRegCost = 10;

    }
}
