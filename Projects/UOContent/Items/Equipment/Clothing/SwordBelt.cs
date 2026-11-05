using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/SwordBelt.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SwordBelt : BaseWaist, IDyable
{
    public override int LabelNumber => 1126021; // sword belt

    [Constructible]
    public SwordBelt()
        : base(0xA40D)
    {
        Weight = 3.0;
        Layer = Layer.Waist;
    }
}
