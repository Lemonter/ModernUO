using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/WolfStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WolfStatue : Item
{
    [Constructible]
    public WolfStatue()
        : base(0x25D3)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1073190; // A Wolf Contribution Statue from the Britannia Royal Zoo.
}
