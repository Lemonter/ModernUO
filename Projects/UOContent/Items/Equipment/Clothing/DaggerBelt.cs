using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/DaggerBelt.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DaggerBelt : BaseWaist, IDyable
{
    public override int LabelNumber => 1159210; // dagger belt

    [Constructible]
    public DaggerBelt()
        : base(0xA40E)
    {
        Weight = 3.0;
        Layer = Layer.Waist;
    }
}
