using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SturdyShovel : BaseHarvestTool
{
    [Constructible]
    public SturdyShovel(int uses = 180) : base(0xF39, uses) => Hue = 0x973;

    public override double DefaultWeight => 5.0;
    public override int LabelNumber => 1045125; // sturdy shovel
    public override HarvestSystem HarvestSystem => Mining.System;

    /// <summary>Лопатой копают землю, а не только руду в скале.</summary>
    public override bool CanExcavate => true;
}
