using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Bestial/BestialGloves.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BestialGloves : LeatherGloves
{
    public override int LabelNumber => 1151198; // Bestial Gloves

    public override int BasePhysicalResistance => 4;

    public override int BaseFireResistance => 19;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 5;

    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public BestialGloves() : base()
    {
        this.Hue = 2010;
        this.Weight = 1;
        this.StrRequirement = 20;
    }
}
