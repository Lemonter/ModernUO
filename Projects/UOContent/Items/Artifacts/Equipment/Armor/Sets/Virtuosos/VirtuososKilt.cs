using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Virtuosos/VirtuososKilt.cs).
///     VirtuososKilt: GargishClothKilt has no counterpart here; re-based on GargishClothKiltType1.
///     VirtuososKilt: Dropped ISetItem — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class VirtuososKilt : GargishClothKiltType1
{
    public override int LabelNumber => 1151559; // Virtuoso's Kilt

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 8;

    public override int BaseColdResistance => 21;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public VirtuososKilt()
    {
        this.Hue = 1374;
        this.Weight = 5;
    }
}
