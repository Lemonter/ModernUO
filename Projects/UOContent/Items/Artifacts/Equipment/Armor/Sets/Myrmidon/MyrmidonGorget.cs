using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Myrmidon/MyrmidonGorget.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MyrmidonGorget : StuddedGorget
{
    [Constructible]
    public MyrmidonGorget()
        : base()
    {
        this.Attributes.BonusStr = 1;
        this.Attributes.BonusHits = 2;

    }

    public override int LabelNumber => 1074306; // Myrmidon Armor

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 3;

    public override int BasePoisonResistance => 5;

    public override int BaseEnergyResistance => 3;
}
