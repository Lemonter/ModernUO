using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Knights/KnightsPlateArms.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KnightsPlateArms : PlateArms
{
    public override int LabelNumber => 1080162; // Knight's Arms

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public KnightsPlateArms() : base()
    {
        this.Hue = 1150;
        this.Weight = 5;

        this.Attributes.BonusHits = 1;

    }
}
