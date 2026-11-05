using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/SummonersKilt.cs).
///     SummonersKilt: GargishClothKilt has no counterpart here; re-based on GargishClothKiltType1.
///     SummonersKilt: Dropped IRepairable — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SummonersKilt : GargishClothKiltType1
{
    public override int LabelNumber => 1113540; // Summoner's Kilt

    [Constructible]
    public SummonersKilt()
    {
        Hue = 1266;
        Attributes.BonusMana = 5;
        Attributes.RegenMana = 2;
        Attributes.SpellDamage = 5;
        Attributes.LowerManaCost = 8;
        Attributes.LowerRegCost = 10;
    }

    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 7;

    public override int BaseColdResistance => 21;

    public override int BasePoisonResistance => 6;

    public override int BaseEnergyResistance => 21;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
