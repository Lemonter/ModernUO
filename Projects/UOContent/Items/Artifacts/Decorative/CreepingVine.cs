using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/CreepingVine.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CreepingVine : Item
{
    public override int LabelNumber => 1112401;

    [Constructible]
    public CreepingVine()
        : base(Utility.Random(18322, 4))
    {
    }
}
