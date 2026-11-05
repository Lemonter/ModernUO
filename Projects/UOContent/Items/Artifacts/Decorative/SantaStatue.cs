using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/SantaStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SantaStatue : MonsterStatuette
{
    public override int LabelNumber => 1097968; // santa statue

    [Constructible]
    public SantaStatue()
        : base(MonsterStatuetteType.Santa)
    {
        Weight = 10.0;
    }

    public override bool ForceShowProperties => true;
}
