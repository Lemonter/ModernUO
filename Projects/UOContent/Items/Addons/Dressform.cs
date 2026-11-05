using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Dressform.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DressformFront : Item
{
    [Constructible]
    public DressformFront()
        : base(0xec6)
    {
        this.Weight = 10;
    }
}
[SerializationGenerator(0, false)]
public partial class DressformSide : Item
{
    [Constructible]
    public DressformSide()
        : base(0xec7)
    {
        Weight = 10;
    }
}
