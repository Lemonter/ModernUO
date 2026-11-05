using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/BakeKitsuneStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BakeKitsuneStatue : Item
{
    [Constructible]
    public BakeKitsuneStatue()
        : base(0x2763)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1073189; // A Bake Kitsune Contribution Statue from the Britannia Royal Zoo.
}
