using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/OstardTopiary.cs).
/// </summary>
[Flippable(0x497D, 0x497E)]
[SerializationGenerator(0, false)]
public partial class OstardTopiary : Item
{
    public override int LabelNumber => 1070878; // a decorative topiary

    [Constructible]
    public OstardTopiary() : base(0x497D)
    {
        this.Weight = 1.0;
        this.Name = ("an ostard topiary");
    }
}
