using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Bestial/BestialKilt.cs).
///     BestialKilt: GargishClothKilt has no counterpart here; re-based on GargishClothKiltType1.
///     BestialKilt: Dropped ISetItem — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BestialKilt : GargishClothKiltType1
{
    public override int LabelNumber => 1151546; // Bestial Kilt

    public override int BasePhysicalResistance => 24;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 9;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 9;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public BestialKilt()
    {
        this.Hue = 2010;
        this.Weight = 5;
    }
}
