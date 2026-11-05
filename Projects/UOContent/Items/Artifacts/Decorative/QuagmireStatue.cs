using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/QuagmireStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class QuagmireStatue : Item
{
    [Constructible]
    public QuagmireStatue()
        : base(0x2614)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 1.0;
    }

    public override int LabelNumber => 1073195; // A Quagmire Contribution Statue from the Britannia Royal Zoo.
}
