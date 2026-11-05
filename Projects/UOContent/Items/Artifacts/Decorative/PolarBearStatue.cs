using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/PolarBearStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PolarBearStatue : Item
{
    [Constructible]
    public PolarBearStatue()
        : base(0x20E1)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1073193; // A Polar Bear Contribution Statue from the Britannia Royal Zoo.
}
