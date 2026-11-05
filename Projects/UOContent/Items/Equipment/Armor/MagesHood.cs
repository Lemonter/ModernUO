using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Armor/MagesHood.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MagesHood : BaseHat
{
    public override int LabelNumber => 1159227; // mage's hood

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 3;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 8;
    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 20;
    public override int InitMaxHits => 40;

    [Constructible]
    public MagesHood()
        : this(0)
    {
    }

    public MagesHood(int hue)
        : base(0xA411, hue)
    {
        Weight = 3.0;
        StrRequirement = 10;
    }
}
