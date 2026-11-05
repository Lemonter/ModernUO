using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Bestial/BestialGorget.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BestialGorget : LeatherGorget
{
    public override int LabelNumber => 1151200; // Bestial Gorget

    public override int BasePhysicalResistance => 6;

    public override int BaseFireResistance => 20;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public BestialGorget() : base()
    {
        this.Hue = 2010;
        this.Weight = 1;
        this.StrRequirement = 25;
    }
}
