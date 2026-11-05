using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Sorcerers/SorcererHat.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SorcererHat : WizardsHat
{
    public override int LabelNumber => 1080465; // Sorcerer's Hat

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public SorcererHat() : base()
    {
        this.Hue = 1165;
        this.Weight = 1;

        this.Attributes.BonusInt = 1;
        this.Attributes.LowerRegCost = 10;

    }
}
