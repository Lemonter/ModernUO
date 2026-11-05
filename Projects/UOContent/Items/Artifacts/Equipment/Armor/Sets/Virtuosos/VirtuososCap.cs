using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtuosos/VirtuososCap.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VirtuososCap : JesterHat
{
    public override int LabelNumber => 1151320; // Virtuoso's Cap

    public override int BasePhysicalResistance => 3;

    public override int BaseFireResistance => 8;

    public override int BaseColdResistance => 23;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public VirtuososCap() : base()
    {
        this.Hue = 1374;
        this.Weight = 5;
        this.StrRequirement = 10;
    }
}
