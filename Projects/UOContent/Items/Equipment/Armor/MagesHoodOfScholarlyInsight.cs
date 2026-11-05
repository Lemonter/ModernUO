using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/MagesHoodOfScholarlyInsight.cs).
///     Dropped (absent here): IsArtifact.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MagesHoodOfScholarlyInsight : MagesHood
{
    public override int LabelNumber => 1159229; // mage's hood of scholarly insight

    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 15;
    public override int BaseColdResistance => 15;
    public override int BasePoisonResistance => 15;
    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public MagesHoodOfScholarlyInsight()
        : this(0)
    {
    }

    public MagesHoodOfScholarlyInsight(int hue)
        : base(hue)
    {
        Attributes.BonusMana = 15;
        Attributes.RegenMana = 2;
        Attributes.SpellDamage = 15;
        Attributes.CastSpeed = 1;
        Attributes.LowerManaCost = 10;
    }
}
