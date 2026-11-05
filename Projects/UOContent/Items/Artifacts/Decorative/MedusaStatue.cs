using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/MedusaStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MedusaStatue : Item
{
    [Constructible]
    public MedusaStatue()
        : base(0x40BC)
    {
        this.Name = "Медуза";
        this.Weight = 10;
    }

    public override int LabelNumber => 1113626;
}
