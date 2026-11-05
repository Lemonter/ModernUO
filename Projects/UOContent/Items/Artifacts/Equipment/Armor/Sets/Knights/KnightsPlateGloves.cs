using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Knights/KnightsPlateGloves.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KnightsPlateGloves : PlateGloves
{
    public override int LabelNumber => 1080161; // Knight's Gloves

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public KnightsPlateGloves() : base()
    {
        this.Hue = 1150;
        this.Weight = 2;

        this.Attributes.BonusHits = 1;

    }
}
