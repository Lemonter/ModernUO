using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/MaceBelt.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MaceBelt : BaseWaist, IDyable
{
    public override int LabelNumber => 1126020; // mace belt

    [Constructible]
    public MaceBelt()
        : base(0xA40C)
    {
        Weight = 3.0;
        Layer = Layer.Waist;
    }
}
