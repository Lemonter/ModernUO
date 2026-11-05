using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/PosedGoblinTopiary.cs).
/// </summary>
[Flippable(0x4960, 0x4966)]
[SerializationGenerator(0, false)]
public partial class PosedGoblinTopiary : Item
{
     public override int LabelNumber => 1070878; // a decorative topiary

    [Constructible]
    public PosedGoblinTopiary() : base(0x4960)
    {
        this.Weight = 1.0;
        this.Name = ("a posed goblin topiary");
    }
}
