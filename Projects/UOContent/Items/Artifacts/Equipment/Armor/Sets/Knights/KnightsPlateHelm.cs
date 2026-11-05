using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Knights/KnightsPlateHelm.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KnightsPlateHelm : PlateHelm
{
    public override int LabelNumber => 1080158; // Knight's Plate Helm

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public KnightsPlateHelm() : base()
    {
        this.Hue = 1150;
        this.Weight = 5;

        this.Attributes.BonusHits = 1;

    }
}
