using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Knights/KnightsPlateGorget.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KnightsPlateGorget : PlateGorget
{
    public override int LabelNumber => 1080160; // Knight's Gorget

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 7;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public KnightsPlateGorget() : base()
    {
        this.Hue = 1150;
        this.Weight = 2;

        this.Attributes.BonusHits = 1;

    }
}
