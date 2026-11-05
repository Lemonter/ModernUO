using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/ReptalonStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ReptalonStatue : Item
{
    [Constructible]
    public ReptalonStatue()
        : base(0x2D95)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1073192; // A Reptalon Contribution Statue from the Britannia Royal Zoo.
}
