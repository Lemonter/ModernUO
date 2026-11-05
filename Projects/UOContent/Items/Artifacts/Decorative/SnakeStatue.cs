using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/SnakeStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SnakeStatue : Item
{
    [Constructible]
    public SnakeStatue()
        : base(0x25C2)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1073194; // A Snake Contribution Statue from the Britannia Royal Zoo.
}
