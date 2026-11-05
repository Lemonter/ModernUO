using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtuosos/VirtuososCollar.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VirtuososCollar : LeatherGorget
{
    public override int LabelNumber => 1151555; // Virtuoso's Collar

    public override int BasePhysicalResistance => 4;

    public override int BaseFireResistance => 19;

    public override int BaseColdResistance => 5;

    public override int BasePoisonResistance => 5;

    public override int BaseEnergyResistance => 5;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public VirtuososCollar() : base()
    {
        this.Hue = 1374;
        this.Weight = 1;
        this.StrRequirement = 20;
    }
}
