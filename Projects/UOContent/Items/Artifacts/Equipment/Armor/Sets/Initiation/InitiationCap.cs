using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Initiation/InitiationCap.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class InitiationCap : LeatherCap
{
    public override int LabelNumber => 1116255; // Armor of Initiation

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 4;

    public override int BasePoisonResistance => 6;

    public override int BaseEnergyResistance => 4;

    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    [Constructible]
    public InitiationCap() : base()
    {
        Weight = 1;
        this.Hue = 0x9C4;
        this.LootType = LootType.Blessed;

    }
}
