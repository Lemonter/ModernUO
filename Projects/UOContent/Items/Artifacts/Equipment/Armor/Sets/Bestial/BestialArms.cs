using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Bestial/BestialArms.cs).
///     BestialArms: GargishLeatherArms has no counterpart here; re-based on GargishLeatherArmsType1.
///     BestialArms: Dropped ISetItem — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BestialArms : GargishLeatherArmsType1
{
    public override int LabelNumber => 1151545; // Bestial Arms

    public override int BasePhysicalResistance => 7;

    public override int BaseFireResistance => 8;

    public override int BaseColdResistance => 21;

    public override int BasePoisonResistance => 8;

    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 125;

    public override int InitMaxHits => 125;

    [Constructible]
    public BestialArms()
    {
        this.ItemID = 0x4052;
        this.Hue = 2010;
        this.Weight = 4;
        this.StrRequirement = 25;
    }
}
