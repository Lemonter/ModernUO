using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/ChangelingStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ChangelingStatue : Item
{
    [Constructible]
    public ChangelingStatue()
        : base(0x2D8A)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1073191; // A Changeling Contribution Statue from the Britannia Royal Zoo.
}
