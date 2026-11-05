using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items;

[Flippable(0x9E7E, 0x9E7F)]
[SerializationGenerator(0, false)]
public partial class ImprovedRockHammer : BaseHarvestTool
{
    [Constructible]
    public ImprovedRockHammer(int uses = 500) : base(0x9E7E, uses)
    {
        Hue = 2721;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1157177; // Improved Rock Hammer

    public override HarvestSystem HarvestSystem => Mining.System;
}
