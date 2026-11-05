using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtuosos/VirtuososTunic.cs).
///     VirtuososTunic: Dropped ISetItem — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class VirtuososTunic : StuddedChest
{
    public override int LabelNumber => 1151321; // Virtuoso's Tunic

    public override int BasePhysicalResistance => 6;

    public override int BaseFireResistance => 20;

    public override int BaseColdResistance => 7;

    public override int BasePoisonResistance => 7;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public VirtuososTunic()
    {
        this.Hue = 1374;
        this.StrRequirement = 35;
    }
}
