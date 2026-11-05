using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Jewelry/GargishBracelet.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishBracelet : BaseBracelet
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishBracelet()
        : base(0x4211)
    {
        //Weight = 0.1;
    }
}
