using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Knights/KnightsFemalePlateChest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KnightsFemalePlateChest : FemalePlateChest
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
    public KnightsFemalePlateChest() : base()
    {
        this.Hue = 1150;
        this.Weight = 4;

        this.Attributes.BonusHits = 1;

    }
}
