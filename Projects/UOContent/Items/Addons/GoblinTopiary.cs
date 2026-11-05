using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GoblinTopiary.cs).
/// </summary>
[Flippable(0x4919, 0x491A)]
[SerializationGenerator(0, false)]
public partial class GoblinTopiary : Item
{
    public override int LabelNumber => 1070878; // a decorative topiary

    [Constructible]
    public GoblinTopiary() : base(0x4919)
    {
        this.Weight = 1.0;
        this.Name = ("a goblin topiary");
    }
}
