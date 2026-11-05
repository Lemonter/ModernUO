using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Bestial/BestialHelm.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BestialHelm : BearMask
{
    public override int LabelNumber => 1151197; // Bestial Helm

    public override int BasePhysicalResistance => 8;

    public override int BaseFireResistance => 6;

    public override int BaseColdResistance => 22;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public BestialHelm() : base()
    {
        this.Hue = 2010;
        this.Weight = 5;
        this.StrRequirement = 10;
    }
}
