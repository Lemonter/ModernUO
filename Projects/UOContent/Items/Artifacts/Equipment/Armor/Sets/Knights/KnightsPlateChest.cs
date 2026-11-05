using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Knights/KnightsPlateChest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KnightsPlateChest : PlateChest
{
    public override int LabelNumber => 1080164; // Knight's Breastplate

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public KnightsPlateChest() : base()
    {
        this.Hue = 1150;
        this.Weight = 10;

        this.Attributes.BonusHits = 1;

    }
}
